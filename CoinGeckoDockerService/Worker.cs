using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CoinGeckoDockerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly CentralizedLoggerClient _centralizedLogger;

        public Worker(
            ILogger<Worker> logger,
            IHttpClientFactory httpClientFactory,
            IServiceProvider serviceProvider,
            CentralizedLoggerClient centralizedLogger)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _serviceProvider = serviceProvider;
            _centralizedLogger = centralizedLogger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await _centralizedLogger.SendLog(LogLevel.Information, $"Worker running at: {DateTimeOffset.Now}");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        await GetDataFromApiAndSaveToDb(dbContext);
                    }

                    await Task.Delay(180000, stoppingToken);
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Unhandled error during CoinGeckoWorker execution: {ex.Message}";
                    _logger.LogError(errorMessage);
                    await _centralizedLogger.SendLog(LogLevel.Error, errorMessage);
                }
            }
        }

        private async Task GetDataFromApiAndSaveToDb(AppDbContext dbContext)
        {
            try
            {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Kryptowaluty/1.0.0");
                var response = await httpClient.GetAsync("https://api.coingecko.com/api/v3/coins/markets?vs_currency=usd");
                var responseContent = await response.Content.ReadAsStringAsync();
                var cryptoList = JsonSerializer.Deserialize<List<Crypto>>(responseContent);
                
                foreach (var crypto in cryptoList)
                {
                    var cryptoId = dbContext.CryptoNames
                        .AsNoTracking()
                        .Where(cn => cn.Symbol == crypto.Symbol && cn.CryptoName == crypto.CryptoName)
                        .Select(cn => cn.Id)
                        .FirstOrDefault();
                    
                    if (cryptoId == 0)
                    {
                        var newCryptoName = new CryptoNames
                        {
                            CryptoName = crypto.CryptoName,
                            Symbol = crypto.Symbol,
                            Image = crypto.Image
                        };
                        dbContext.CryptoNames.Add(newCryptoName);
                        await dbContext.SaveChangesAsync(); // Wymusza wygenerowanie Id 
                        cryptoId = newCryptoName.Id;    
                    }
                    
                    var cryptoEntity = new Crypto
                    {
                        High24 = crypto.High24,
                        Low24 = crypto.Low24,
                        CryptoPrice = crypto.CryptoPrice,
                        PriceChange = crypto.PriceChange,
                        DateTime = DateTime.Now,
                        Crypto_Id = cryptoId
                    };

                    dbContext.Cryptos.Add(cryptoEntity);
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (HttpRequestException httpEx)
            {
                var warningMessage = $"Network issue while calling API: {httpEx.Message}";
                _logger.LogWarning(warningMessage);
                await _centralizedLogger.SendLog(LogLevel.Warning, warningMessage);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Critical error occurred while fetching or saving data: {ex.Message}";
                _logger.LogError(errorMessage);
                await _centralizedLogger.SendLog(LogLevel.Error, errorMessage);
            }
        }
    }
}