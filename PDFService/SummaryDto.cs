using System.Text.Json.Serialization;

namespace PDFService;

public class SummaryDto
{
    [JsonPropertyName("crypto")]
    public LogEntry Crypto { get; set; }

    [JsonPropertyName("exchangeRates")]
    public LogEntry ExchangeRates { get; set; }

    [JsonPropertyName("cryptoNames")]
    public LogEntry CryptoNames { get; set; }

    [JsonPropertyName("currencyNames")]
    public LogEntry CurrencyNames { get; set; }

    [JsonPropertyName("totalRecords")]
    public LogEntry TotalRecords { get; set; }
}