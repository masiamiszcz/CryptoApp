// Worker.cs
using System.Text.Json;
using System.Xml.Linq;
using CurrencyService.AppDb;
using CurrencyService.CurrencyModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CurrencyService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider; 
        private readonly CentralizedLoggerClient _centralizedLogger;

        public Worker(
            ILogger<Worker> logger,
            IServiceProvider serviceProvider,
            CentralizedLoggerClient centralizedLogger)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _centralizedLogger = centralizedLogger; 
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var message = $"Currency Worker running at: {DateTimeOffset.Now}";
            _logger.LogInformation(message);
            await _centralizedLogger.SendLog(LogLevel.Information, message);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    await GetDataFromApi(dbContext, 1, "https://api.nbp.pl/api/exchangerates/tables/A/");
                    await GetDataFromApi(dbContext, 2, "https://api.nbp.pl/api/exchangerates/tables/B/");
                    await GetDataFromApi(dbContext, 3, "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml");
                    await GetDataFromApi(dbContext, 4,
                        "http://data.fixer.io/api/latest?access_key=7af2bef3df0bacaa34e167bb4f214403");

                    await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
                }
                catch (Exception e)
                {
                    var errorMessage = $"Unexpected error in Worker: {e.Message}";
                    _logger.LogError(errorMessage);
                    await _centralizedLogger.SendLog(LogLevel.Error, errorMessage);
                    throw;
                }
            }
        }

        private async Task GetDataFromApi(AppDbContext dbContext, int apiId, string url)
        {
            if (!CanFetchData(dbContext, apiId))
            {
                var fetchMessage = $"No fetching necessary for API ID: {apiId}";
                _logger.LogInformation(fetchMessage);
                await _centralizedLogger.SendLog(LogLevel.Information, fetchMessage);
                return;
            }

            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "CurrencyService");

                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = $"API {apiId} returned an error: {(int)response.StatusCode} - {response.ReasonPhrase}";
                    throw new Exception(errorResponse);
                }

                var content = await response.Content.ReadAsStringAsync();

                switch (apiId)
                {
                    case 1:
                        _logger.LogInformation("Processing NBP data for table A...");
                        await _centralizedLogger.SendLog(LogLevel.Information, "Processing NBP data for table A...");
                        await NBPVoid(dbContext, content, apiId);
                        break;
                    case 2:
                        _logger.LogInformation("Processing NBP data for table B...");
                        await _centralizedLogger.SendLog(LogLevel.Information, "Processing NBP data for table B...");
                        await NBPVoid(dbContext, content, apiId);
                        break;
                    case 3:
                        _logger.LogInformation("Processing EBC data...");
                        await _centralizedLogger.SendLog(LogLevel.Information, "Processing EBC data...");
                        await EBCVoid(dbContext, content);
                        break;
                    case 4:
                        _logger.LogInformation("Processing Fixer.io data...");
                        await _centralizedLogger.SendLog(LogLevel.Information, "Processing Fixer.io data...");
                        await FixerVoid(dbContext, content);
                        break;
                    default:
                        var warningMessage = $"API {apiId} is not recognized.";
                        _logger.LogWarning(warningMessage);
                        await _centralizedLogger.SendLog(LogLevel.Warning, warningMessage);
                        return;
                }
            }
            catch (HttpRequestException httpEx)
            {
                var networkErrorMessage = $"Network problem while calling API Id {apiId}: {httpEx.Message}";
                _logger.LogWarning(networkErrorMessage);
                await _centralizedLogger.SendLog(LogLevel.Warning, networkErrorMessage);
            }
            catch (Exception ex)
            {
                var criticalError = $"Critical error occurred while fetching from API Id {apiId}: {ex.Message}";
                _logger.LogError(criticalError);
                await _centralizedLogger.SendLog(LogLevel.Error, criticalError);
            }
        }

        private bool CanFetchData(AppDbContext dbContext, int apiId)
        {
            var now = DateTime.Now;
            var lastFetchTime = dbContext.ExchangeRates
                .AsNoTracking()
                .Where(r => r.ApiId == apiId)
                .OrderByDescending(r => r.DateTime)
                .Select(r => r.DateTime)
                .FirstOrDefault();

            if (lastFetchTime == default) return true;

            var eightHoursAgo = now.AddHours(-8);
            var today1230 = now.Date.AddHours(12).AddMinutes(30);

            switch (apiId)
            {
                case 1:
                    if (now < today1230)
                        return lastFetchTime < eightHoursAgo;
                    else
                        return lastFetchTime < today1230;
                case 2:
                    return lastFetchTime < today1230;
                case 3:
                    var today1615 = now.Date.AddHours(16).AddMinutes(15);
                    if (now < today1615)
                        return lastFetchTime < eightHoursAgo;
                    else
                        return lastFetchTime < today1615;
                case 4:
                    return lastFetchTime < eightHoursAgo;
                default:
                    _logger.LogWarning($"Unknown API ID: {apiId}. Cannot determine fetch logic.");
                    return false;
            }
        }

        private async Task NBPVoid(AppDbContext dbContext, string jsonContent, int apiId)
        {
            try
            {
                var exchangeTables = JsonSerializer.Deserialize<List<NBPMapper>>(jsonContent);
                if (exchangeTables == null || !exchangeTables.Any())
                {
                    _logger.LogWarning("Brak danych w odpowiedzi z API NBP.");
                    return;
                }

                var baseCurrency = dbContext.CurrencyNames
                    .AsNoTracking()
                    .FirstOrDefault(c => c.Symbol == "PLN")
                    ?? new CurrencyNames { Symbol = "PLN", CurrencyName = "złoty polski" };

                if (baseCurrency.Id == 0)
                {
                    dbContext.CurrencyNames.Add(baseCurrency);
                    await dbContext.SaveChangesAsync();
                }

                int baseCurrencyId = baseCurrency.Id;

                foreach (var table in exchangeTables)
                {
                    foreach (var rate in table.Rates)
                    {
                        if (string.IsNullOrWhiteSpace(rate.Currency) || string.IsNullOrWhiteSpace(rate.Code))
                        {
                            _logger.LogWarning($"Niekompletne dane: Currency='{rate.Currency}', Code='{rate.Code}'");
                            continue;
                        }

                        var targetCurrency = dbContext.CurrencyNames
                            .AsNoTracking()
                            .FirstOrDefault(c => c.Symbol == rate.Code);

                        if (targetCurrency == null)
                        {
                            targetCurrency = new CurrencyNames { Symbol = rate.Code, CurrencyName = rate.Currency };
                            dbContext.CurrencyNames.Add(targetCurrency);
                            await dbContext.SaveChangesAsync();
                        }
                        else if (string.IsNullOrWhiteSpace(targetCurrency.CurrencyName))
                        {
                            var attached = dbContext.CurrencyNames.First(c => c.Id == targetCurrency.Id);
                            attached.CurrencyName = rate.Currency;
                            await dbContext.SaveChangesAsync();
                        }

                        dbContext.ExchangeRates.Add(new ExchangeRates
                        {
                            Id  = targetCurrency.Id,
                            Id2 = baseCurrencyId,
                            ExchangeRate = rate.Mid,
                            ApiId = apiId,
                            DateTime = DateTime.Now
                        });
                    }
                }

                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Dane z API NBP zostały pomyślnie zapisane.");
            }
            catch (Exception ex)
            {
                var criticalError = $"Critical error in NBPVoid (API {apiId}): {ex.Message}";
                _logger.LogError(criticalError);
                await _centralizedLogger.SendLog(LogLevel.Error, criticalError);
            }
        }

        private async Task EBCVoid(AppDbContext dbContext, string xmlContent)
        {
            var document = XDocument.Parse(xmlContent);
            var timeCube = document.Descendants().FirstOrDefault(e => e.Name.LocalName == "Cube" && e.Attribute("time") != null);
            if (timeCube == null) throw new InvalidOperationException("No valid time or rate data in XML.");

            var rateCubes = timeCube.Descendants()
                .Where(e => e.Name.LocalName == "Cube" && e.Attribute("currency") != null && e.Attribute("rate") != null);

            var baseCurrency = dbContext.CurrencyNames.AsNoTracking().FirstOrDefault(c => c.Symbol == "EUR");
            if (baseCurrency == null)
            {
                baseCurrency = new CurrencyNames { Symbol = "EUR", CurrencyName = "Euro" };
                dbContext.CurrencyNames.Add(baseCurrency);
                await dbContext.SaveChangesAsync();
            }

            int baseCurrencyId = baseCurrency.Id;

            foreach (var rateCube in rateCubes)
            {
                var symbol = rateCube.Attribute("currency")!.Value;
                var exchangeRate = decimal.Parse(rateCube.Attribute("rate")!.Value);

                var existing = dbContext.CurrencyNames.AsNoTracking().FirstOrDefault(c => c.Symbol == symbol);
                if (existing == null)
                {
                    existing = new CurrencyNames { Symbol = symbol, CurrencyName = null };
                    dbContext.CurrencyNames.Add(existing);
                    await dbContext.SaveChangesAsync();
                }
                int targetCurrencyId = existing.Id;

                dbContext.ExchangeRates.Add(new ExchangeRates
                {
                    Id  = baseCurrencyId,
                    Id2 = targetCurrencyId,
                    ExchangeRate = exchangeRate,
                    ApiId = 3,
                    DateTime = DateTime.Now
                });
            }

            await dbContext.SaveChangesAsync();
        }

        private async Task FixerVoid(AppDbContext dbContext, string jsonContent)
        {
            var fixerData = JsonSerializer.Deserialize<FixerMapper>(jsonContent)
                            ?? throw new InvalidOperationException("Invalid JSON from Fixer.io");

            var baseCurrency = dbContext.CurrencyNames.AsNoTracking()
                .FirstOrDefault(c => c.Symbol == fixerData.Base)
                             ?? new CurrencyNames { Symbol = fixerData.Base, CurrencyName = "Euro" };

            if (baseCurrency.Id == 0)
            {
                dbContext.CurrencyNames.Add(baseCurrency);
                await dbContext.SaveChangesAsync();
            }

            int baseCurrencyId = baseCurrency.Id;

            var predefined = new Dictionary<string, string>
            {
                { "BMD", "Dolar bermudzki" },
                { "BTN", "Ngultrum bhutański" },
                { "BTC", "Bitcoin (częściowo prawny)" },
                { "KPW", "Won północnokoreański" },
                { "CLF", "Chilijska jednostka rozliczeniowa UF" },
                { "CNH", "Juan offshore" },
                { "FKP", "Funt Wysp Falklandzkich" },
                { "GGP", "Funt Guernsey" },
                { "IMP", "Funt Wyspy Man" },
                { "JEP", "Funt Jersey" },
                { "KYD", "Dolar Kajmanów" },
                { "SHP", "Funt Wyspy Świętej Heleny" },
                { "SLL", "Leone Sierra Leone" },
                { "ZWL", "Dolar Zimbabwe" }
            };

            foreach (var kv in fixerData.Rates)
            {
                var symbol = kv.Key;
                var rate   = kv.Value;

                var existing = dbContext.CurrencyNames.AsNoTracking().FirstOrDefault(c => c.Symbol == symbol);
                if (existing == null)
                {
                    if (!predefined.TryGetValue(symbol, out var name)) continue;
                    existing = new CurrencyNames { Symbol = symbol, CurrencyName = name };
                    dbContext.CurrencyNames.Add(existing);
                    await dbContext.SaveChangesAsync();
                }
                else if (string.IsNullOrWhiteSpace(existing.CurrencyName) && predefined.TryGetValue(symbol, out var upd))
                {
                    var attached = dbContext.CurrencyNames.First(c => c.Id == existing.Id);
                    attached.CurrencyName = upd;
                    await dbContext.SaveChangesAsync();
                }

                dbContext.ExchangeRates.Add(new ExchangeRates
                {
                    Id  = baseCurrencyId,
                    Id2 = existing.Id,
                    ExchangeRate = rate,
                    ApiId = 4,
                    DateTime = DateTime.Now
                });
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
