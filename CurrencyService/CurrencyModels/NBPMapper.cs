using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CurrencyService.AppDb;

// Klasa odwzorowująca strukturę danych z API NBP
public class NBPMapper
{
    [JsonPropertyName("table")]
    public string Table { get; set; } // Typ tabeli, np. "A" lub "B"

    [JsonPropertyName("no")]
    public string No { get; set; } // Numer tabeli

    [JsonPropertyName("effectiveDate")]
    public string EffectiveDate { get; set; } // Data publikacji tabeli

    [JsonPropertyName("rates")]
    public List<NBPCurrencyRate> Rates { get; set; } // Lista kursów
}

// Klasa reprezentująca pojedynczy kurs waluty
public class NBPCurrencyRate
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } // Nazwa waluty

    [JsonPropertyName("code")]
    public string Code { get; set; } // Kod waluty (np. USD, EUR)

    [JsonPropertyName("mid")]
    public decimal Mid { get; set; } // Średni kurs waluty
}