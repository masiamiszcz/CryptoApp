using CurrencyService.CurrencyModels;

namespace CurrencyService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Currency Worker running at: {time}", DateTimeOffset.Now);

            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await FetchCurrencyData(dbContext, "1", "https://api.nbp.pl/api/exchangerates/tables/A/");
                await FetchCurrencyData(dbContext, "2", "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml");
                await FetchCurrencyData(dbContext, "3", "http://data.fixer.io/api/latest?access_key=your_api_key");
            }

            // Co minutę sprawdzamy, czy należy wykonać kolejne pobranie
            await Task.Delay(60000, stoppingToken);
        }
    }

    private async Task GetDataFromApi(AppDbContext dbContext, int apiId, string url)
    {
        try
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Ezipies", "CurrencyService");
            var response = await httpClient.GetAsync("https://api.nbp.pl/api/exchangerates/tables/A/");
            var content = await response.Content.ReadAsStringAsync();
            var CurrencyList = 
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error occurred while fetching or saving data: {ex.Message}");
        }
    }

    private bool CanFetchData(AppDbContext dbContext, string apiName)
    {
        var now = DateTime.Now;

        // Pobierz datę ostatniego pobrania w tym API
        var lastFetchTime = dbContext.ExchangeRates
            .Where(r => r.ApiName == apiName)
            .OrderByDescending(r => r.Date)
            .Select(r => r.Date)
            .FirstOrDefault();

        if (lastFetchTime == default)
        {
            // Jeśli nigdy nie pobrano, zezwól na pobranie
            return true;
        }

        // Określ przedziały czasowe
        var timeWindows = new List<(TimeSpan Start, TimeSpan End)>
        {
            (new TimeSpan(4, 30, 0), new TimeSpan(12, 30, 0)), // 4:30 - 12:30
            (new TimeSpan(12, 30, 0), new TimeSpan(20, 30, 0)), // 12:30 - 20:30
            (new TimeSpan(20, 30, 0), new TimeSpan(4, 30, 0)) // 20:30 - 4:30 (następny dzień)
        };
        
        var currentTime = now.TimeOfDay;
        
        foreach (var window in timeWindows)
        {
            if (currentTime >= window.Start && currentTime < window.End)
            {
                // Jeśli w przedziale czasowym, sprawdź ostatnie pobranie
                return lastFetchTime.TimeOfDay < window.Start || lastFetchTime.TimeOfDay >= window.End;
            }
        }

        return false;
    }
    private List<ExchangeRates> ParseNbpResponse(string content)
    {
        var lista = new List<ExchangeRates>();
        // Deserializuj dane XML z NBP
        // Dopasuj format danych do modelu ExchangeRates
        return new List<ExchangeRates>(); // Przykład
    }

    private List<ExchangeRates> ParseEbcResponse(string content)
    {
        var lista = new List<ExchangeRates>();
        // Zmień dane XML z EBC na List<ExchangeRates>
        return new List<ExchangeRates>(); // Przykład
    }

    private List<ExchangeRates> ParseFixerResponse(string content)
    { 
        
        
        var lista = new List<ExchangeRates>(); // Przykład
        // Deserializuj dane JSON z Fixer.io
        return ;
    }
}