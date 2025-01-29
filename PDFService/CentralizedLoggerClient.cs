using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class CentralizedLoggerClient
{
    private readonly HttpClient _client;
    private readonly ILogger<CentralizedLoggerClient> _localLogger;
    private readonly string _loggerUrl = "http://logger-service:8500"; // Logger URL na stałe w kodzie

    public CentralizedLoggerClient(HttpClient client, ILogger<CentralizedLoggerClient> localLogger)
    {
        _client = client;
        _localLogger = localLogger;

        // Ustawienie BaseAddress na sztywny adres URL
        _client.BaseAddress = new Uri(_loggerUrl);
    }

    public async Task SendLog(LogLevel level, string message)
    {
        var logEntry = new
        {
            Level = level,
            Message = message,
            Timestamp = DateTime.UtcNow,
            LogSource = 9
        };

        var json = JsonSerializer.Serialize(logEntry);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _client.PostAsync("/api/logs", content);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Logger API returned status code {response.StatusCode}");
            }

            _localLogger.LogInformation("Successfully sent log to central logger: {Message}", message);
        }
        catch (Exception ex)
        {
            _localLogger.LogError(ex, "Failed to send log to central logger. Falling back to local logging.");
            LogFallback(level, message);
        }
    }

    private void LogFallback(LogLevel level, string message)
    {
        _localLogger.Log(level, "Fallback log: {Message}", message);
        
    }
}