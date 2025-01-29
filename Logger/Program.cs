using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Logger;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Jawne włączenie Logger.appsettings.json
        builder.Configuration.AddJsonFile("Logger.appsettings.json", optional: false, reloadOnChange: true);


        builder.Host.UseSerilog((context, configuration) => configuration
            .WriteTo.Console()
            .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
            .ReadFrom.Configuration(context.Configuration));

        // Konfiguracja Kestrel
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(8500); // Ustawienie portu Kestrel na 8500
        });

        var dbFolderpath = "C:\\data";
        var dbfolder = "C:\\data\\logs";
        var pdffolder = "C:\\data\\pdfs";
        var backupfolder = "C:\\data\\backups";
        if (!Directory.Exists(dbFolderpath))
        {
            Directory.CreateDirectory(dbFolderpath);
        }
        if (!Directory.Exists(dbfolder))
        {
            Directory.CreateDirectory(dbfolder);
        }

        if (!Directory.Exists(backupfolder))
        {
            Directory.CreateDirectory(backupfolder);
        }

        if (!Directory.Exists(pdffolder))
        {
            Directory.CreateDirectory(pdffolder);
        }

        builder.Services.AddDbContext<LogsDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("LogsDb")));

        // Dodanie kontrolerów MVC
        builder.Services.AddControllers();
        //builder.Services.AddHostedService<PdfWorker>();

        var app = builder.Build();

        // Pobranie instancji ILogger
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LogsDbContext>();

            try
            {
                // Jawne logowanie ścieżki do pliku bazy danych
                var connectionString = app.Configuration.GetConnectionString("LogsDb");
                logger.LogInformation("Ścieżka do pliku bazy danych: {ConnectionString}", connectionString);

                logger.LogInformation("Rozpoczęcie migracji bazy danych o godzinie {Time}", DateTime.Now);
                dbContext.Database.Migrate(); // Wykonanie migracji
                logger.LogInformation("Migracja bazy danych zakończona pomyślnie o godzinie {Time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Błąd podczas migracji bazy danych: {Message}", ex.Message);
            }
        }
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}