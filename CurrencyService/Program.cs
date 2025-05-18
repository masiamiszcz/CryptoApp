using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using CurrencyService;
using CurrencyService.CurrencyModels;
using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateBuilder(args);

// 1) Rejestracja usług
builder.Services.AddHttpClient<CentralizedLoggerClient>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHostedService<Worker>();

// 2) HttpClientFactory dla zewnętrznych API
builder.Services.AddHttpClient("NBPA",    c => c.BaseAddress = new Uri("https://api.nbp.pl/api/exchangerates/tables/A/"));
builder.Services.AddHttpClient("NBPB",    c => c.BaseAddress = new Uri("https://api.nbp.pl/api/exchangerates/tables/B/"));
builder.Services.AddHttpClient("ECB",     c => c.BaseAddress = new Uri("https://www.ecb.europa.eu/stats/eurofxref/"));
builder.Services.AddHttpClient("FixerIO", c => c.BaseAddress = new Uri("http://data.fixer.io/api/"));

// 3) Swagger i API explorer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Currency Service API",
        Version     = "v1",
        Description = "Pobiera kursy z NBP, ECB i Fixer.io"
    });
});

// 4) CORS dla Swagger UI
builder.Services.AddCors(options =>
{
    options.AddPolicy("SwaggerUI", policy =>
        policy.WithOrigins("http://localhost:8080")
              .AllowAnyHeader()
              .AllowAnyMethod()
    );
});

// 5) Nasłuchiwanie 0.0.0.0:5010 (zgodnie z docker-compose)
builder.WebHost.UseUrls("http://0.0.0.0:5010");

var app = builder.Build();

// 6) Middleware
app.UseRouting();

// 7) Enable CORS before Swagger
app.UseCors("SwaggerUI");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Currency Service v1");
    c.RoutePrefix = "swagger";
});

// 8) Minimalne API
app.MapGet("/api/rates/nbp/A", async (IHttpClientFactory f) =>
{
    var r = await f.CreateClient("NBPA").GetAsync("");
    r.EnsureSuccessStatusCode();
    var json = await r.Content.ReadAsStringAsync();
    return Results.Content(json, "application/json");
})
.WithName("GetNBPTableA")
.WithTags("NBP");

app.MapGet("/api/rates/nbp/B", async (IHttpClientFactory f) =>
{
    var r = await f.CreateClient("NBPB").GetAsync("");
    r.EnsureSuccessStatusCode();
    var json = await r.Content.ReadAsStringAsync();
    return Results.Content(json, "application/json");
})
.WithName("GetNBPTableB")
.WithTags("NBP");

app.MapGet("/api/rates/ecb/daily", async (IHttpClientFactory f) =>
{
    var r = await f.CreateClient("ECB").GetAsync("eurofxref-daily.xml");
    r.EnsureSuccessStatusCode();
    var xml = await r.Content.ReadAsStringAsync();
    return Results.Content(xml, "application/xml");
})
.WithName("GetEcbDaily")
.WithTags("ECB");

app.MapGet("/api/rates/fixer/latest", async (IHttpClientFactory f) =>
{
    var r = await f.CreateClient("FixerIO").GetAsync("latest?access_key=7af2bef3df0bacaa34e167bb4f214403");
    r.EnsureSuccessStatusCode();
    var json = await r.Content.ReadAsStringAsync();
    return Results.Content(json, "application/json");
})
.WithName("GetFixerLatest")
.WithTags("FixerIO");

app.MapPost("/api/logs/test", async (CentralizedLoggerClient logger) =>
{
    await logger.SendLog(LogLevel.Information, "Testowy log z CurrencyService via API");
    return Results.Ok(new { status = "sent" });
})
.WithName("SendTestLog")
.WithTags("Logs");

// 9) Health i redirect
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok("Healthy"));

// 10) Uruchomienie
app.Run();
