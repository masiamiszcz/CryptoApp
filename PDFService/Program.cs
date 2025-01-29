using Logger;

namespace PDFService;

public class Program
{
    public static void Main(string[] args)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var builder = Host.CreateApplicationBuilder(args);
        builder.Configuration.AddJsonFile("PDF.appsettings.json", optional: false, reloadOnChange: true);
        builder.Services.AddHttpClient<CentralizedLoggerClient>();
        builder.Services.AddHostedService<PdfWorker>();

        var host = builder.Build();
        host.Run();
    }
}