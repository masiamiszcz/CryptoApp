using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using PDFService;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1) Licencja QuestPDF
QuestPDF.Settings.License = LicenseType.Community;

// 2) Wczytanie ustawień
builder.Configuration.AddJsonFile("PDF.appsettings.json", optional: false, reloadOnChange: true);

// 3) Rejestracja usług
builder.Services.AddHttpClient<CentralizedLoggerClient>();
builder.Services.AddHostedService<PdfWorker>();

// 4) Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PDF Service API",
        Version = "v1",
        Description = "Generuje raporty PDF na podstawie logów"
    });
});

// 5) Ustaw nasłuchiwanie w Dockerze
builder.WebHost.UseUrls("http://0.0.0.0:5030");

// 6) Dodanie CORS – pozwól na zapytania z Swagger UI
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:8080")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// 7) Middleware
app.UseRouting();

// 8) Włącz CORS przed Swagger
app.UseCors();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PDF Service v1");
    c.RoutePrefix = "swagger";
});

// 9) Minimalne endpointy
app.MapPost("/api/logs/test", async (CentralizedLoggerClient logger) =>
{
    await logger.SendLog(LogLevel.Information, "Testowy log z PDFService via API");
    return Results.Ok(new { status = "sent" });
})
.WithName("SendTestLog")
.WithTags("Logs");

app.MapPost("/api/pdf/generate", () => Results.Ok(new { status = "queued" }))
    .WithName("GeneratePdf")
    .WithTags("PDF");

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();


