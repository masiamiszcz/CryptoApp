using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Logger.Db;

public class LogEntry
{
    [BindNever] 
    public int Id { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int LogSource { get; set; }
}