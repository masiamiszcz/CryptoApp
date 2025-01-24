using System.Text.Json.Serialization;

namespace CurrencyService.AppDb;

public class FixerMapper
{
    [JsonPropertyName("base")]
    public string Base { get; set; }

    [JsonPropertyName("rates")]
    public Dictionary<string, decimal> Rates { get; set; }
}