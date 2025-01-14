using System.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using CryptoDbDockerService.AppDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CryptoDbDockerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<Worker>();
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            var host = builder.Build();
            host.Run();
            /*if (ApplyDatabaseUpdate(host.Services))
            {
                Thread.Sleep(10000);
            

            
            }*/
        }

        /*private static bool ApplyDatabaseUpdate(IServiceProvider services)
        {
            var maxRetries = 10;
            var retryDelay = TimeSpan.FromSeconds(30);
            var i = 0;
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            for (; i < maxRetries; i++)
            {
                try
                {
                    //dbContext.Database.Migrate();
                    if (i == 4)
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    Thread.Sleep(retryDelay);
                }
            }

            if (i == maxRetries - 1)
            {
                Environment.Exit(1); // Zatrzymaj aplikację, jeśli baza danych nie jest gotowa
            }

            return false;
        }

        /*private static void ApplyLatestMigrations(IServiceProvider services, ILogger logger)
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            try
            {
                logger.LogInformation("Wykonywanie migracji...");
                //dbContext.Database.Migrate();
                logger.LogInformation("Migracje zostały zastosowane.");
            }
            catch (Exception ex)
            {
                logger.LogInformation($"Błąd podczas wykonywania migracji: {ex.Message}");
                throw;
            }
        }*/
    }
}