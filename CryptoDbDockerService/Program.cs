using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using CryptoDbDockerService;
using CryptoDbDockerService.AppDb;
using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateBuilder(args);

// 1) Konfiguracja ustawień
builder.Configuration.AddJsonFile("CryptoDb.appsettings.json", optional: false, reloadOnChange: true);

// 2) Zapewnij katalog backupów lokalnie (w hoście)
var path = "/data";
if (!Directory.Exists(path)) Directory.CreateDirectory(path);

// 3) Rejestracja usług
builder.Services.AddHttpClient<CentralizedLoggerClient>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHostedService<Worker>();

// 4) Swagger i API explorer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CryptoDb Service",
        Version = "v1",
        Description = "Zarządza backupem, restore i statystykami CryptoDb"
    });
});

// 5) CORS dla Swagger UI
builder.Services.AddCors(options =>
{
    options.AddPolicy("SwaggerUI", policy =>
        policy.WithOrigins("http://localhost:8080")
              .AllowAnyHeader()
              .AllowAnyMethod()
    );
});

// 6) Nasłuchiwanie na 0.0.0.0:5020
builder.WebHost.UseUrls("http://0.0.0.0:5020");

var app = builder.Build();

// 7) Middleware pipeline
app.UseRouting();

// 8) Enable CORS before Swagger
app.UseCors("SwaggerUI");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CryptoDb Service v1");
    c.RoutePrefix = "swagger";
});

// 9) Minimalne API
// Endpoint testowego loga
app.MapPost("/api/logs/test", async (CentralizedLoggerClient logger) =>
{
    await logger.SendLog(LogLevel.Information, "Testowy log z CryptoDbService via API");
    return Results.Ok(new { status = "sent" });
})
.WithName("SendTestLog")
.WithTags("Logs");

// Health i przekierowanie
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok("Healthy"));

// 10) Start aplikacji
app.Run();
