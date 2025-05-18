using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using CoinGeckoDockerService;
using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateBuilder(args);

// 1) Konfiguracja pliku ustawień
builder.Configuration.AddJsonFile("CoinService.appsettings.json", optional: false, reloadOnChange: true);

// 2) Rejestracja HttpClient, DbContext i Worker
builder.Services.AddHttpClient<CentralizedLoggerClient>();
builder.Services.AddHttpClient("CoinGeckoClient", client =>
{
    client.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHostedService<Worker>();

// 3) Dodajemy kontrolery (potrzebne, by Swagger zobaczył minimalne API)
builder.Services.AddControllers();

// 4) CORS dla Swagger UI
builder.Services.AddCors(options =>
{
    options.AddPolicy("SwaggerUI", policy =>
        policy.WithOrigins("http://localhost:8080")
              .AllowAnyHeader()
              .AllowAnyMethod()
    );
});

// 5) Swagger: dokumentacja API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CoinGecko Docker Service API",
        Version = "v1",
        Description = "Pobiera i zapisuje dane z CoinGecko"
    });
});

// 6) Nasłuchuj na wszystkich interfejsach:5000
builder.WebHost.UseUrls("http://0.0.0.0:5000");

var app = builder.Build();

// 7) Middleware
app.UseRouting();

// 8) Enable CORS BEFORE Swagger middleware
app.UseCors("SwaggerUI");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CoinGecko v1");
    c.RoutePrefix = "swagger";
});

// 9) Mapowanie endpointów
app.MapControllers();

//  ➤ Minimalne API: pobranie top 100 coinów
app.MapGet("/api/coins/top100", async (IHttpClientFactory httpFactory) =>
{
    var client = httpFactory.CreateClient("CoinGeckoClient");
    var resp = await client.GetAsync("https://api.coingecko.com/api/v3/coins/markets?vs_currency=usd");
    resp.EnsureSuccessStatusCode();
    var json = await resp.Content.ReadAsStringAsync();
    return Results.Content(json, "application/json");
})
.WithName("GetTop100Coins")
.WithTags("CoinGecko");

//  ➤ Minimalne API: wysyłka testowego loga
app.MapPost("/api/logs/test", async (CentralizedLoggerClient logger) =>
{
    await logger.SendLog(LogLevel.Information, "Testowy log via API");
    return Results.Ok(new { status = "sent" });
})
.WithName("SendTestLog")
.WithTags("Logs");

// 10) Health i przekierowanie
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok("Healthy"));

// 11) Start
app.Run();
