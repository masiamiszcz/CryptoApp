using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Logger.Db;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Logger.Controllers
{
    [ApiController]
    [Route("api/logs")]
    public class LogController : ControllerBase
    {
        private readonly LogsDbContext _dbContext;

        public LogController(LogsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentLogs()
        {
            var dateThreshold = DateTime.UtcNow.AddHours(-8);

            var logs = await _dbContext.Logs
                .Where(log => log.Timestamp >= dateThreshold)
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();

            return Ok(logs);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var cryptoLog = await _dbContext.Logs
                .Where(log => log.Message.Contains("Crypto table has"))
                .OrderByDescending(log => log.Timestamp)
                .FirstOrDefaultAsync();

            var exchangeRatesLog = await _dbContext.Logs
                .Where(log => log.Message.Contains("ExchangeRates table has"))
                .OrderByDescending(log => log.Timestamp)
                .FirstOrDefaultAsync();

            var cryptoNamesLog = await _dbContext.Logs
                .Where(log => log.Message.Contains("CryptoNames table has"))
                .OrderByDescending(log => log.Timestamp)
                .FirstOrDefaultAsync();

            var currencyNamesLog = await _dbContext.Logs
                .Where(log => log.Message.Contains("CurrencyNames table has"))
                .OrderByDescending(log => log.Timestamp)
                .FirstOrDefaultAsync();
    
            var totallogs = await _dbContext.Logs
                .Where(log => log.Message.Contains("Total number of records"))
                .OrderByDescending(log => log.Timestamp)
                .FirstOrDefaultAsync();
            var summary = new
            {
                Crypto = cryptoLog,
                ExchangeRates = exchangeRatesLog,
                CryptoNames = cryptoNamesLog,
                CurrencyNames = currencyNamesLog,
                TotalRecords = totallogs,
            };

            return Ok(summary);
        }
    }
}