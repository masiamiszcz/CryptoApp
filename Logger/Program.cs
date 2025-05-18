using System;
using System.IO;
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

        // Utworzenie folderów na hostcie
        var folders = new[] { "C:\\data", "C:\\data\\logs", "C:\\data\\pdfs", "C:\\data\\backups" };
        foreach (var f in folders)
        {
            if (!Directory.Exists(f))
                Directory.CreateDirectory(f);
        }

        // Rejestracja DbContext
        builder.Services.AddDbContext<LogsDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("LogsDb")));

        builder.Services.AddControllers();

        // CORS dla Swagger UI
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("SwaggerUI", policy =>
                policy.WithOrigins("http://localhost:8080")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
            );
        });

        // Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Logger",
                Version = "v1",
                Description = "Logger Documents"
            });
        });

        var app = builder.Build();

        // Logowanie i migracja DB
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LogsDbContext>();
            try
            {
                var connectionString = app.Configuration.GetConnectionString("LogsDb");
                logger.LogInformation("Ścieżka do pliku bazy danych: {ConnectionString}", connectionString);
                logger.LogInformation("Rozpoczęcie migracji bazy danych o godzinie {Time}", DateTime.Now);
                dbContext.Database.Migrate();
                logger.LogInformation("Migracja bazy danych zakończona pomyślnie o godzinie {Time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Błąd podczas migracji bazy danych: {Message}", ex.Message);
            }
        }

        app.UseRouting();

        // Włącz CORS przed Swagger
        app.UseCors("SwaggerUI");

        app.UseHttpsRedirection();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Logger API V1");
            c.RoutePrefix = string.Empty;
        });

        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
