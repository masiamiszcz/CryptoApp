using Logger.Db;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly LogsDbContext _dbContext; // DbContext dla bazy
    private readonly ILogger<LogsController> _logger; // Logger do obsługi logów

    public LogsController(ILogger<LogsController> logger, LogsDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<IActionResult> PostLog([FromBody] LogEntry logEntry)
    {
        if (logEntry == null || string.IsNullOrWhiteSpace(logEntry.Message))
        {
            return BadRequest("Invalid log entry.");
        }

        try
        {
            // Zapisanie logu do bazy danych
            await _dbContext.Logs.AddAsync(logEntry);
            await _dbContext.SaveChangesAsync();

            // Opcjonalnie logowanie w konsoli aplikacji
            _logger.Log(logEntry.Level, "Log saved: {Timestamp} - {Message}", logEntry.Timestamp, logEntry.Message);
        }
        catch (Exception ex)
        {
            // Logowanie błędu, jeśli zapis do bazy nie powiedzie się
            _logger.LogError(ex, "Failed to save log entry to the database.");
            return StatusCode(500, "An error occurred while saving the log.");
        }

        return Ok("Log successfully saved to the database.");
    }
}