using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoDbDockerService.AppDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CryptoDbDockerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly CentralizedLoggerClient _centralizedLogger;
        private readonly IServiceProvider _serviceProvider;

        public Worker(ILogger<Worker> logger, CentralizedLoggerClient centralizedLogger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _centralizedLogger = centralizedLogger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);
                await _centralizedLogger.SendLog(LogLevel.Information, $"Worker started at: {DateTimeOffset.Now}");

                await RestoreDatabaseAsync(); 

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    await CheckCryptoTableAsync();
                    await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
                    await BackupDatabaseAsync();
                }
            }
            catch (Exception ex)
            {
                var message = $"Unhandled error in worker: {ex.Message}";
                _logger.LogError(message);
                await _centralizedLogger.SendLog(LogLevel.Error, message);
            }
        }

        private async Task RestoreDatabaseAsync()
        {
            try
            {
                var backupDirectory = "/data";
                if (!Directory.Exists(backupDirectory))
                {
                    _logger.LogInformation("Backup directory does not exist. Skipping restore.");
                    return;
                }

                var backupFile = Directory.GetFiles(backupDirectory, "*.bak")
                                          .OrderByDescending(File.GetCreationTime)
                                          .FirstOrDefault();

                if (backupFile == null)
                {
                    _logger.LogInformation("No backup files found. Skipping restore. Applying migraion ...");
                    await ApplyMigrationsAsync();
                    return;
                } 

                _logger.LogInformation("Restoring database from: {fileName}", backupFile);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var connectionString = dbContext.Database.GetConnectionString();

                    // Tworzymy nowe połączenie, zamiast zmieniać istniejące
                    using (var connection = new Microsoft.Data.SqlClient.SqlConnection(
                               connectionString.Replace($"Database={dbContext.Database.GetDbConnection().Database};", "Database=master;")))
                    {
                        await connection.OpenAsync();
                        using (var command = connection.CreateCommand())
                        {
                            // Przywrócenie bazy danych
                            command.CommandText = $@"
                RESTORE DATABASE [CryptoDb]
                FROM DISK = '/data/{Path.GetFileName(backupFile)}'
                WITH FILE = 1, REPLACE, NOUNLOAD, STATS = 5;
            ";
                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    _logger.LogInformation("Database restored successfully from: {fileName}", backupFile);
                    await _centralizedLogger.SendLog(LogLevel.Information, $"Database restored successfully from: {backupFile}");
                }
            }
            catch (Exception ex)
            {
                var message = $"Error during database restore: {ex.Message}";
                _logger.LogError(message);
                await _centralizedLogger.SendLog(LogLevel.Error, message);
            }
        }
        

        private async Task ApplyMigrationsAsync()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    _logger.LogInformation("Applying database migrations...");
                    await _centralizedLogger.SendLog(LogLevel.Information, "Applying database migrations...");

                    await dbContext.Database.MigrateAsync(); // Uruchom migracje

                    _logger.LogInformation("Database migrations applied successfully.");
                    await _centralizedLogger.SendLog(LogLevel.Information, "Database migrations applied successfully.");
                }
            }
            catch (Exception ex)
            {
                var message = $"Migration error: {ex.Message}";
                _logger.LogError(message);
                await _centralizedLogger.SendLog(LogLevel.Error, message);
                throw; // Nie pozwól kontynuować, jeśli migracje się nie powiodły
            }
        }



        private async Task CheckCryptoTableAsync()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var recordCountCrypto = await dbContext.Cryptos.CountAsync();
                    await _centralizedLogger.SendLog(LogLevel.Information, $"Crypto table has: {recordCountCrypto} records.");
                    var recordCountRates = await dbContext.ExchangeRates.CountAsync();
                    await _centralizedLogger.SendLog(LogLevel.Information, $"ExchangeRates table has: {recordCountRates} records.");
                    var recordCountCryptoNames = await dbContext.CryptoNames.CountAsync();
                    await _centralizedLogger.SendLog(LogLevel.Information, $"CryptoNames table has: {recordCountCryptoNames} records.");
                    var recordCountCurrencyNames = await dbContext.CurrencyNames.CountAsync();
                    await _centralizedLogger.SendLog(LogLevel.Information, $"CurrencyNames table has: {recordCountCurrencyNames} records.");
                    var recordCount = recordCountCrypto + recordCountRates + recordCountCryptoNames + recordCountCurrencyNames;

                    _logger.LogInformation("Total records: {count}", recordCount);
                    await _centralizedLogger.SendLog(LogLevel.Information, $"Total number of records: {recordCount}");
                }
            }
            catch (Exception ex)
            {
                var message = $"Error checking crypto table: {ex.Message}";
                _logger.LogError(message);
                await _centralizedLogger.SendLog(LogLevel.Error, message);
            }
        }

        private async Task BackupDatabaseAsync()
        {
            try
            {
                var backupDirectory = "/data";
                if (!Directory.Exists(backupDirectory))
                {
                    Directory.CreateDirectory(backupDirectory);
                }

                var backupFileName = Path.Combine(backupDirectory, $"Backup_{DateTime.Now:yyyyMMddHHmmss}.bak");
                _logger.LogInformation("Starting database backup to file: {fileName}", backupFileName);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var connectionString = dbContext.Database.GetConnectionString();

                    using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = $@"
                                BACKUP DATABASE [{dbContext.Database.GetDbConnection().Database}]
                                TO DISK = '/data/{Path.GetFileName(backupFileName)}'
                                WITH FORMAT, INIT, SKIP, NOREWIND, NOUNLOAD, STATS = 10;
                            ";
                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    _logger.LogInformation("Database backup completed: {fileName}", backupFileName);
                    await _centralizedLogger.SendLog(LogLevel.Information, $"Database backup completed: {backupFileName}");
                }
            }
            catch (Exception ex)
            {
                var message = $"Error during database backup: {ex.Message}";
                _logger.LogError(message);
                await _centralizedLogger.SendLog(LogLevel.Error, message);
            }
        }
    }
}
