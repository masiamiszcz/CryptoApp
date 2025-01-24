using System.Text.Json;
using System.Xml.Linq;
using System.Xml.Serialization;
using CryptoDbDockerService.AppDb;
using CurrencyService.AppDb;
using CurrencyService.CurrencyModels;
using Microsoft.EntityFrameworkCore;
using AppDbContext = CurrencyService.CurrencyModels.AppDbContext;
using CurrencyNames = CurrencyService.CurrencyModels.CurrencyNames;
using ExchangeRates = CurrencyService.CurrencyModels.ExchangeRates;

namespace CurrencyService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider; 
    private readonly CentralizedLoggerClient _centralizedLogger;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, CentralizedLoggerClient centralizedLogger)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _centralizedLogger = centralizedLogger; 
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = $"Currency Worker running at: {DateTimeOffset.Now}";
                _logger.LogInformation(message);
                await _centralizedLogger.SendLog(LogLevel.Information, message);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    await GetDataFromApi(dbContext, 1, "https://api.nbp.pl/api/exchangerates/tables/A/");
                    await GetDataFromApi(dbContext, 2, "https://api.nbp.pl/api/exchangerates/tables/B/");
                    await GetDataFromApi(dbContext, 3, "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml");
                    await GetDataFromApi(dbContext, 4,
                        "http://data.fixer.io/api/latest?access_key=7af2bef3df0bacaa34e167bb4f214403");
                }

                // Co 5 min sprawdzamy, czy należy wykonać kolejne pobranie
                await Task.Delay(300000, stoppingToken);
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
            var httpClient = new HttpClient();
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

        // Pobierz datę i czas ostatniego pobrania dla danego API
        var lastFetchTime = dbContext.ExchangeRates
            .AsNoTracking()
            .Where(r => r.ApiId == apiId)
            .OrderByDescending(r => r.DateTime)
            .Select(r => r.DateTime)
            .FirstOrDefault();

        // Jeśli nie było wcześniej pobrania, zawsze wykonujemy pierwsze pobranie
        if (lastFetchTime == default)
        {
            return true;
        }

        // Logika warunków dla poszczególnych API
        switch (apiId)
        {
            case 1: // API NBP
                // Jeśli dzisiaj po 12:30 nie było pobrania, należy pobrać dane
                var today1230 = now.Date.AddHours(12).AddMinutes(30);
                if (lastFetchTime < today1230)
                {
                    return true;
                }
                break;
            case 2: // API NBP
                // Jeśli dzisiaj po 12:30 nie było pobrania, należy pobrać dane
                    today1230 = now.Date.AddHours(12).AddMinutes(30);
                if (lastFetchTime < today1230)
                {
                    return true;
                }
                break;

            case 3: // API EBC
                // Jeśli dzisiaj po 16:15 nie było pobrania, należy pobrać dane
                var today1615 = now.Date.AddHours(16).AddMinutes(15);
                if (lastFetchTime < today1615)
                {
                    return true;
                }
                break;

            case 4: // API Fixer.io
                // Jeśli od ostatniego pobrania minęło więcej niż 8 godzin, należy pobrać dane
                var eightHoursAgo = now.AddHours(-8);
                if (lastFetchTime < eightHoursAgo)
                {
                    return true;
                }
                break;

            default:
                _logger.LogWarning($"Unknown API ID: {apiId}. Cannot determine fetch logic.");
                return false;
        }

        // Jeśli żaden warunek nie został spełniony, nie należy pobierać danych
        return false;
    }
private async Task NBPVoid(AppDbContext dbContext, string jsonContent, int apiId)
{
    try
    {
        // Deserializacja danych NBP JSON do obiektów mapera
        var exchangeTables = JsonSerializer.Deserialize<List<NBPMapper>>(jsonContent);

        if (exchangeTables == null || !exchangeTables.Any())
        {
            _logger.LogWarning("Brak danych w odpowiedzi z API NBP.");
            return;
        }

        // Pobierz lub dodaj bazową walutę PLN do tabeli `CurrencyNames`
        var baseCurrency = dbContext.CurrencyNames
            .AsNoTracking()
            .FirstOrDefault(c => c.Symbol == "PLN")
            ?? new CurrencyNames
            {
                Symbol = "PLN",
                CurrencyName = "złoty polski"
            };

        if (baseCurrency.Id == 0)
        {
            dbContext.CurrencyNames.Add(baseCurrency);
            await dbContext.SaveChangesAsync();
        }

        int baseCurrencyId = baseCurrency.Id;

        // Iteracja przez otrzymane tabele kursów
        foreach (var table in exchangeTables)
        {
            foreach (var rate in table.Rates)
            {
                // Weryfikacja poprawności danych
                if (string.IsNullOrWhiteSpace(rate.Currency) || string.IsNullOrWhiteSpace(rate.Code))
                {
                    _logger.LogWarning($"Niekompletne dane: Currency='{rate.Currency}', Code='{rate.Code}'");
                    continue;
                }

                // Pobierz lub dodaj docelową walutę do tabeli `CurrencyNames`
                var targetCurrency = dbContext.CurrencyNames
                    .AsNoTracking()
                    .FirstOrDefault(c => c.Symbol == rate.Code);

                if (targetCurrency == null)
                {
                    // Jeśli waluty brak, dodaj nową walutę
                    targetCurrency = new CurrencyNames
                    {
                        Symbol = rate.Code,
                        CurrencyName = rate.Currency
                    };
                    dbContext.CurrencyNames.Add(targetCurrency);
                    await dbContext.SaveChangesAsync();
                }
                else if (string.IsNullOrWhiteSpace(targetCurrency.CurrencyName))
                {
                    var attachedCurrency = dbContext.CurrencyNames.First(c => c.Id == targetCurrency.Id);
                    attachedCurrency.CurrencyName = rate.Currency;
                    await dbContext.SaveChangesAsync();
                }

                int targetCurrencyId = targetCurrency.Id;

                // Tworzenie nowego rekordu w tabeli `ExchangeRates`
                var exchangeRate = new ExchangeRates
                {
                    Id = targetCurrencyId,
                    Id2 = baseCurrencyId,
                    ExchangeRate = rate.Mid,
                    ApiId = apiId, // NBP API
                    DateTime = DateTime.Now
                };

                dbContext.ExchangeRates.Add(exchangeRate);
            }
        }

        // Zapis wszystkich zmian w bazie
        await dbContext.SaveChangesAsync();
        _logger.LogInformation("Dane z API NBP zostały pomyślnie zapisane.");
    }
    catch (Exception ex)
    {
        var criticalError = $"Critical error occurred while fetching from API Id {apiId}: {ex.Message}";
        _logger.LogError(criticalError);
        await _centralizedLogger.SendLog(LogLevel.Error, criticalError);
    }
}

    private async Task EBCVoid(AppDbContext dbContext, string xmlContent)
{
    // Wczytaj dane z XML
    var document = XDocument.Parse(xmlContent);

    // Znajdź wszystkie elementy <Cube> z kursem
    var timeCube = document.Descendants()
        .FirstOrDefault(e => e.Name.LocalName == "Cube" && e.Attribute("time") != null);

    if (timeCube == null)
        throw new InvalidOperationException("No valid time or rate data found in the XML file.");

    var rateCubes = timeCube.Descendants()
        .Where(e => e.Name.LocalName == "Cube" && e.Attribute("currency") != null && e.Attribute("rate") != null);

    // Waluta bazowa: EUR (na sztywno, w EBC zawsze tak jest)
    var baseCurrency = dbContext.CurrencyNames
        .AsNoTracking()
        .FirstOrDefault(c => c.Symbol == "EUR");

    if (baseCurrency == null)
    {
        baseCurrency = new CurrencyNames
        {
            Symbol = "EUR",
            CurrencyName = "Euro"
        };
        dbContext.CurrencyNames.Add(baseCurrency);
        dbContext.SaveChanges();
    }

    // Pobierz Id waluty bazowej
    int baseCurrencyId = baseCurrency.Id;

    // Iteracja przez dane o kursach
    foreach (var rateCube in rateCubes)
    {
        
        var symbol = rateCube.Attribute("currency")?.Value;
        var exchangeRate = decimal.Parse(rateCube.Attribute("rate")?.Value);

        

        // Sprawdź, czy waluta istnieje w tabeli CurrencyNames
        var existingCurrency = dbContext.CurrencyNames
            .AsNoTracking()
            .FirstOrDefault(c => c.Symbol == symbol);

        if (existingCurrency == null)
        {
            // Dodaj walutę, jeśli jej brak (nazwa jest null w tym API)
            existingCurrency = new CurrencyNames
            {
                Symbol = symbol,
                CurrencyName = null // Brak nazwy waluty w XML
            };
            dbContext.CurrencyNames.Add(existingCurrency);
            dbContext.SaveChanges();
        }

        // Pobierz Id waluty docelowej
        int targetCurrencyId = existingCurrency.Id;

        // Dodaj rekord do ExchangeRates
        var newExchangeRate = new ExchangeRates
        {
            Id = baseCurrencyId,
            Id2 = targetCurrencyId,
            ExchangeRate = exchangeRate,
            ApiId = 3, // API EBC
            DateTime = DateTime.Now
        };

        dbContext.ExchangeRates.Add(newExchangeRate);
    }

    // Zapisz zmiany w bazie danych
    dbContext.SaveChanges();
}

    private async Task FixerVoid(AppDbContext dbContext, string jsonContent)
    {
        var fixerData = JsonSerializer.Deserialize<FixerMapper>(jsonContent);

        if (fixerData == null)
            throw new InvalidOperationException("Invalid or malformed JSON data.");

        // Pobierz lub dodaj bazową walutę "EUR" do CurrencyNames
        var baseCurrency = dbContext.CurrencyNames
            .AsNoTracking()
            .FirstOrDefault(c => c.Symbol == fixerData.Base);

        if (baseCurrency == null)
        {
            baseCurrency = new CurrencyNames
            {
                Symbol = fixerData.Base,
                CurrencyName = "Euro"
            };
            dbContext.CurrencyNames.Add(baseCurrency);
            dbContext.SaveChanges();
        }
        var predefinedCurrencyNames = new Dictionary<string, string>
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
        // Pobierz Id waluty bazowej
        int baseCurrencyId = baseCurrency.Id;

        // Przetwórz dane z sekcji "rates"
        foreach (var rate in fixerData.Rates)
        {
            // Pobierz lub dodaj walutę do CurrencyNames
            var currencySymbol = dbContext.CurrencyNames
                .AsNoTracking()
                .FirstOrDefault(c => c.Symbol == rate.Key);

            if (currencySymbol == null)
            {
                // Jeśli waluty nie ma w bazie, sprawdź, czy jest w predefiniowanej liście
                if (!predefinedCurrencyNames.TryGetValue(rate.Key, out var currencyName) || string.IsNullOrWhiteSpace(currencyName))
                {
                    // Pominięcie waluty, jeśli brak nazwy
                    _logger.LogWarning($"Pominięto walutę {rate.Key}: brak nazwy w liście.");
                    continue;
                }

                // Dodaj nową walutę do bazy
                currencySymbol = new CurrencyNames
                {
                    Symbol = rate.Key,
                    CurrencyName = currencyName
                };
                dbContext.CurrencyNames.Add(currencySymbol);
                await dbContext.SaveChangesAsync();
            }
            else if (string.IsNullOrWhiteSpace(currencySymbol.CurrencyName))
            {
                // Jeśli waluta istnieje, ale jej nazwa to null - uzupełnij nazwę, jeśli jest w liście
                if (!predefinedCurrencyNames.TryGetValue(rate.Key, out var updatedCurrencyName) || string.IsNullOrWhiteSpace(updatedCurrencyName))
                {
                    // Pominięcie waluty, jeśli nadal brak nazwy
                    _logger.LogWarning($"Pominięto walutę {rate.Key}: brak nazwy podczas aktualizacji.");
                    continue;
                }

                // Aktualizacja nazwy w istniejącym rekordzie
                var attachedCurrency = dbContext.CurrencyNames.First(c => c.Id == currencySymbol.Id);
                attachedCurrency.CurrencyName = updatedCurrencyName;
                await dbContext.SaveChangesAsync();
            }

            // Pobierz Id waluty docelowej
            int targetCurrencyId = currencySymbol.Id;

            // Dodaj rekord do ExchangeRates
            var exchangeRate = new ExchangeRates
            {
                Id = baseCurrencyId,
                Id2 = targetCurrencyId,
                ExchangeRate = rate.Value,
                ApiId = 4,
                DateTime = DateTime.Now
            };

            dbContext.ExchangeRates.Add(exchangeRate);
        }

        // Zapisz zmiany w bazie danych
        dbContext.SaveChanges();
    }
}