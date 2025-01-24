using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Logger;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(8500); // Ustawienie portu Kestrel na 8500
        });

        var dbFolder = "C:\\data";
        if (!Directory.Exists(dbFolder))
        {
            Directory.CreateDirectory(dbFolder);
        }
        builder.Services.AddDbContext<LogsDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("LogsDb")));

        // Dodanie kontrolerów MVC
        builder.Services.AddControllers();

        var app = builder.Build();
        
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LogsDbContext>();
            dbContext.Database.Migrate(); // Automatyczna migracja
        }
        
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}