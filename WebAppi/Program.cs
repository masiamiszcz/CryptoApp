using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using WebAppi;
using WebAppi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1) Rejestracja HttpClient do loggera
builder.Services.AddHttpClient<CentralizedLoggerClient>();

// 2) MVC: kontrolery z widokami
builder.Services.AddControllersWithViews();

// 3) DbContext dla SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 4) Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WebAppi API",
        Version = "v1",
        Description = "Webowe API dla aplikacji z widokami"
    });
});

// 5) Ustaw nasłuchiwanie w Dockerze
builder.WebHost.UseUrls("http://0.0.0.0:8050");

// 6) Dodanie CORS
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

// 7) Konfiguracja potoku HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 8) Włącz CORS
app.UseCors();

app.UseAuthorization();

// 9) Middleware Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebAppi v1");
    c.RoutePrefix = string.Empty; // Root dostępny od razu na /
});

// 10) Mapowanie kontrolerów i widoków
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 11) Minimalne endpointy
app.MapGet("/health", () => Results.Ok("Healthy"));
app.MapPost("/api/logs/test", async (CentralizedLoggerClient logger) =>
{
    await logger.SendLog(LogLevel.Information, "Test log from WebAppi via API");
    return Results.Ok(new { status = "sent" });
})
.WithName("SendTestLog")
.WithTags("Logs");
app.MapGet("/", () => Results.Redirect("/swagger"));

// 12) Uruchomienie aplikacji
app.Run();
