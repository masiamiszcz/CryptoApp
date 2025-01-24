using Microsoft.EntityFrameworkCore;

namespace CoinGeckoDockerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Configuration.AddJsonFile("CoinService.appsettings.json", optional: false, reloadOnChange: true);

            var configuration = builder.Configuration;

           
            builder.Services.AddHttpClient<CentralizedLoggerClient>();

            // Rejestracja klienta do CoinGecko API
            builder.Services.AddHttpClient("CoinGeckoClient", client =>
            {
                client.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
            });

            // Rejestracja DbContext z użyciem connection string z appsettings.json
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Rejestracja Worker Service
            builder.Services.AddHostedService<Worker>();

            var host = builder.Build();
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    // Rozszerzenie konfiguracji (np. możliwość zmiany pliku JSON bez ponownej rekompilacji)
                    config.AddJsonFile("CoinService.appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = hostContext.Configuration;

                    // Rejestracja HttpClientFactory
                    services.AddHttpClient();

                    // Rejestracja AppDbContext (baza danych)
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

                    // Rejestracja Worker Service
                    services.AddHostedService<Worker>();
                });
    }
}