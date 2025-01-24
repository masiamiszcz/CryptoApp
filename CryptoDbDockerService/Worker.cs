namespace CryptoDbDockerService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly CentralizedLoggerClient _centralizedLogger;

    public Worker(ILogger<Worker> logger, CentralizedLoggerClient centralizedLogger)
    {
        _logger = logger;
        _centralizedLogger = centralizedLogger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    await _centralizedLogger.SendLog(LogLevel.Information, $"Worker running at: {DateTimeOffset.Now}");
                }

                await Task.Delay(1000, stoppingToken);
            }
            catch (Exception ex)
            {
                var message = $"Unhandled error in worker loop: {ex.Message}";
                _logger.LogError(message);
                await _centralizedLogger.SendLog(LogLevel.Error, message);
            }
        }
    }
}