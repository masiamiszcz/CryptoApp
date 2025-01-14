using CurrencyService.CurrencyModels;
using Microsoft.EntityFrameworkCore;

namespace CurrencyService;

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
    }
}