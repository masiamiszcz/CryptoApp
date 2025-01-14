using Microsoft.EntityFrameworkCore;

namespace CoinGeckoDockerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Wczytanie konfiguracji z appsettings.json
            var configuration = builder.Configuration;

            // Rejestracja HttpClientFactory
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
                    // Wczytywanie ustawień aplikacji (jeśli potrzebujesz rozszerzenia)
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
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