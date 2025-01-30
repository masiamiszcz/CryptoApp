using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PDFService;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PDFService
{
    public class PdfWorker : BackgroundService
    {
        private const string PdfFolder = "/data/pdfs"; // Folder wewnątrz kontenera
        private readonly ILogger<PdfWorker> _logger; // Logger
        private readonly HttpClient _httpClient; // HttpClient do komunikacji z Logger API
        private readonly CentralizedLoggerClient _centralizedLogger; // Kontekst bazy danych

        public PdfWorker(ILogger<PdfWorker> logger, HttpClient httpClient, CentralizedLoggerClient centralizedLogger)
        {
            _logger = logger;
            _httpClient = httpClient;
            _centralizedLogger = centralizedLogger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PDF Worker uruchomiony w: {time}", DateTimeOffset.Now);
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PdfCreating();
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

                    var sleepInterval = TimeSpan.FromHours(8); // 8 godzin
                    _logger.LogInformation("Ponowne sprawdzenie za {interval} godzin.", sleepInterval.TotalHours);

                    await Task.Delay(sleepInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Wystąpił błąd podczas pracy PDF Worker: {ex.Message}";
                    _logger.LogError(errorMessage);
                }
            }
        }
        private async Task PdfCreating()
        {
            try
            {
                _logger.LogInformation("Rozpoczęcie procesu generowania PDF.");
        
                // Utworzenie folderu, jeśli nie istnieje
                if (!Directory.Exists(PdfFolder))
                {
                    Directory.CreateDirectory(PdfFolder);
                    _logger.LogInformation($"Folder PDF został utworzony: {PdfFolder}");
                }
        
                // Pobranie wszystkich plików PDF w folderze, posortowanie od najnowszych
                var pdfFiles = new DirectoryInfo(PdfFolder)
                    .GetFiles("*.pdf")
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();
        
                // Znalezienie najnowszego pliku i sprawdzenie czasu jego utworzenia
                if (pdfFiles.Any())
                {
                    var newestFile = pdfFiles.First();
                    var timeSinceLastWrite = DateTime.Now - newestFile.CreationTime;
        
                    if (timeSinceLastWrite.TotalHours < 8)
                    {
                        var message = $"Plik PDF {newestFile.Name} istnieje i nie jest starszy niż 8 godzin. Następna aktualizacja możliwa za {8 - timeSinceLastWrite.TotalHours:F2} godziny.";
                        _logger.LogInformation(message);
                        return;
                    }
                }
        
                // Usuwanie najstarszych plików, jeśli liczba plików przekracza  50
                while (pdfFiles.Count >= 50)
                {
                    var oldestFile = pdfFiles.Last(); // Najstarszy plik
                    oldestFile.Delete(); // Usuwanie pliku
                    _logger.LogInformation($"Usunięto stary plik PDF: {oldestFile.Name}");
                    pdfFiles.Remove(oldestFile);
                }
        
                // Generowanie unikalnej nazwy pliku PDF
                var pdfFileName = $"Raport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"; 
                var pdfFilePath = Path.Combine(PdfFolder, pdfFileName);

                await CreatePdf(pdfFilePath);
        
                var successMessage = $"Plik PDF został wygenerowany w lokalizacji: {pdfFilePath}";
                _logger.LogInformation(successMessage);
                await _centralizedLogger.SendLog(LogLevel.Information, successMessage);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Błąd w trakcie generowania pliku PDF: {ex.Message}";
                _logger.LogError(errorMessage);
                await _centralizedLogger.SendLog(LogLevel.Error, errorMessage);
            }
        }

         private async Task CreatePdf(string filePath)
        {
            try
            {
                // Pobranie logów z Logger API
                _logger.LogInformation("Pobieranie danych z Logger API...");

                // Pobranie logów z ostatnich 8 godzin
                var recentLogsResponse = await _httpClient.GetAsync("http://logger-service:8500/api/logs/recent");
                recentLogsResponse.EnsureSuccessStatusCode();
                var recentLogsJson = await recentLogsResponse.Content.ReadAsStringAsync();
                var recentLogs = JsonSerializer.Deserialize<List<LogEntry>>(recentLogsJson);
                var filteredLogs = recentLogs?.Where(log => log.Level == 3 || log.Level == 4).ToList();
                _logger.LogInformation($"Po filtrowaniu pozostało {filteredLogs?.Count} logów (Warning/Error).");

                var summaryResponse = await _httpClient.GetAsync("http://logger-service:8500/api/logs/summary");
                summaryResponse.EnsureSuccessStatusCode();
                var summaryJson = await summaryResponse.Content.ReadAsStringAsync();
                var summary = JsonSerializer.Deserialize<SummaryDto>(summaryJson);

                _logger.LogInformation("Dane z Logger API zostały pomyślnie pobrane.");

                // Tworzenie dokumentu PDF
                var report = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(30);
                        page.Size(PageSizes.A4);

                        // Nagłówek
                        page.Header()
                            .AlignCenter()
                            .Text("Raport systemu Logger")
                            .FontSize(20)
                            .Bold();

                        // Główna zawartość
                        page.Content().Column(column =>
                        {
                            // Data wygenerowania raportu
                            column.Item().AlignLeft()
                                .Text($"Data wygenerowania: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
                                .FontSize(12);

                            column.Item().PaddingVertical(10);

                            // Sekcja podsumowania
                            column.Item().Text("*** Podsumowanie ***")
                                .FontSize(14).Bold();

                            if (summary != null)
                            {
                                column.Item().Text($"{summary.Crypto?.Message}").FontSize(12);
                                column.Item().Text($"{summary.ExchangeRates?.Message}").FontSize(12);
                                column.Item().Text($"{summary.CryptoNames?.Message}").FontSize(12);
                                column.Item().Text($"{summary.CurrencyNames?.Message}").FontSize(12);
                                column.Item().Text($"{summary.TotalRecords?.Message}").FontSize(12);
                            }

                            column.Item().PaddingVertical(10);

                            // Logi Warning i Error
                            column.Item().Text("*** Logi z ostatnich 8 godzin ***")
                                .FontSize(14).Bold();

                            if (filteredLogs != null && filteredLogs.Any())
                            {
                                foreach (var log in filteredLogs)
                                {
                                    string source = log.LogSource switch
                                    {
                                        1 => "DbService",
                                        2 => "CoinGeckoService",
                                        3 => "CurrencyService",
                                        4 => "WebAppi",
                                        9 => "PDFservice",
                                        _ => "Nieznane"
                                    };

                                    string logLevel = log.Level switch
                                    {
                                        3 => "Warning",
                                        4 => "Error",
                                        _ => "Brak Info"
                                    };

                                    column.Item()
                                        .Text($"{log.Timestamp:yyyy-MM-dd HH:mm:ss} UTC: [{logLevel}] {log.Message} (Źródło: {source})")
                                        .FontSize(12);
                                }
                            }
                            else
                            {
                                column.Item().Text("Brak logów na poziomie Warning lub Error.").FontSize(12);
                            }
                        });
                    });
                });

                // Zapisanie PDF
                report.GeneratePdf(filePath);
                _logger.LogInformation("Raport został zapisany do pliku PDF.");
            }
            catch (Exception ex)
            {
                var errorMessage = $"Błąd podczas generowania pliku PDF: {ex.Message}";
                _logger.LogError(errorMessage);
            }
        }
    }
}