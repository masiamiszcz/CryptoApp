using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using CoinGeckoDockerService;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateBuilder(args);

// 1) Konfiguracja pliku ustawień
builder.Configuration.AddJsonFile("CoinService.appsettings.json", optional: false, reloadOnChange: true);

// 2) Rejestracja HttpClient, DbContext i Worker
builder.Services.AddHttpClient<CentralizedLoggerClient>();

builder.Services.AddHttpClient("CoinGeckoClient", client =>
{
    client.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
    // tak jak w Worker.cs
    client.DefaultRequestHeaders.Add("User-Agent", "Kryptowaluty/1.0.0");
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json")
    );
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHostedService<Worker>();

// 3) Kontrolery (Swagger wymaga)
builder.Services.AddControllers();

// 4) CORS dla Swagger UI
builder.Services.AddCors(options =>
{
    options.AddPolicy("SwaggerUI", policy =>
        policy.WithOrigins("http://localhost:8080", "http://localhost:5000")
              .AllowAnyHeader()
              .AllowAnyMethod()
    );
});

// 5) Swagger
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

// 7) Dev exceptions
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseCors("SwaggerUI");
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CoinGecko v1");
    c.RoutePrefix = "swagger";
});

// 8) Mapowanie kontrolerów
app.MapControllers();

// 9) Minimalne API: pobranie top 100 coinów
app.MapGet("/api/coins/top100", async (IHttpClientFactory httpFactory) =>
{
    var client = httpFactory.CreateClient("CoinGeckoClient");
    try
    {
        // używamy relatywnego URI względem BaseAddress
        var resp = await client.GetAsync(
            "coins/markets?" +
            "vs_currency=usd" +
            "&order=market_cap_desc" +
            "&per_page=100" +
            "&page=1" +
            "&sparkline=false"
        );

        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            // zwracamy pełne info: status + ciało
            return Results.Problem(
                detail: $"CoinGecko {(int)resp.StatusCode}: {body}",
                statusCode: (int)resp.StatusCode
            );
        }

        return Results.Content(body, "application/json");
    }
    catch (HttpRequestException ex)
    {
        // logowanie możesz dorzucić tutaj, jeśli chcesz
        return Results.Problem(
            detail: $"Błąd połączenia z CoinGecko: {ex.Message}",
            statusCode: 502
        );
    }
})
.WithName("GetTop100Coins")
.WithTags("CoinGecko");

// 10) Testowy log
app.MapPost("/api/logs/test", async (CentralizedLoggerClient logger) =>
{
    await logger.SendLog(LogLevel.Information, "Testowy log via API");
    return Results.Ok(new { status = "sent" });
})
.WithName("SendTestLog")
.WithTags("Logs");

// 11) Health i przekierowanie
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok("Healthy"));

// 12) Start
app.Run();
