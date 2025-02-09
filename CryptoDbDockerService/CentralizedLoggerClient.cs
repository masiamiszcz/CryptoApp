using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class CentralizedLoggerClient
{
    private readonly HttpClient _client;
    private readonly ILogger<CentralizedLoggerClient> _localLogger;
    private readonly string _loggerUrl = "http://logger-service:8500"; 
    private readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>(); 

    public CentralizedLoggerClient(HttpClient client, ILogger<CentralizedLoggerClient> localLogger)
    {
        _client = client;
        _localLogger = localLogger;


        _client.BaseAddress = new Uri(_loggerUrl);
    }

    public async Task SendLog(LogLevel level, string message)
    {
        var logEntry = new
        {
            Level = level,
            Message = message,
            Timestamp = DateTime.UtcNow,
            LogSource = 1
        };

        var json = JsonSerializer.Serialize(logEntry);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        await TryProcessQueue();
        
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
            _logQueue.Enqueue(json);
            LogFallback(level, message);
        }
    }

    private void LogFallback(LogLevel level, string message)
    {
        _localLogger.Log(level, "Fallback log: {Message}", message);
        
    }
    private async Task TryProcessQueue()
    {
        while (_logQueue.TryPeek(out var log)) // Sprawdź, czy w kolejce są logi
        {
            try
            {
                var content = new StringContent(log, Encoding.UTF8, "application/json");
                var response = await _client.PostAsync("/api/logs", content);

                if (response.IsSuccessStatusCode)
                {
                    _logQueue.TryDequeue(out _); // Usuń log z kolejki, bo wysłano go pomyślnie
                    _localLogger.LogInformation("Successfully resent a log from the retry queue.");
                }
                else
                {
                    _localLogger.LogWarning("Failed to resend a log from the retry queue. Keeping it in queue.");
                    break; // Jeśli API nie działa, przestań przetwarzać kolejkę
                }
            }
            catch
            {
                _localLogger.LogWarning("Failed to resend a log from the retry queue. Keeping it in queue.");
                break; // Jeśli wystąpi wyjątek, przestań przetwarzać kolejkę
            }
        }
    }
}