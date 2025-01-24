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
            builder.Configuration.AddJsonFile("CryptoDb.appsettings.json", optional: false, reloadOnChange: true);
            builder.Services.AddHttpClient<CentralizedLoggerClient>();
            builder.Services.AddHostedService<Worker>();
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            var host = builder.Build();
            host.Run();
            
        }
    }
}