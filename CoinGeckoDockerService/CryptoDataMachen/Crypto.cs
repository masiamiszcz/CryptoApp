using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

public class Crypto
{
    [NotMapped]
    [JsonPropertyName("name")]
    public string CryptoName { get; set; }
    
    [NotMapped]
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; }
    
    [NotMapped]
    [JsonPropertyName("image")]
    public string Image { get; set; }
    
    [JsonPropertyName("high_24h")]
    public decimal High24 { get; set; }
    
    [JsonPropertyName("low_24h")]
    public decimal Low24 { get; set; }
    
    [JsonPropertyName("current_price")]
    public decimal CryptoPrice { get; set; }
    
    [JsonPropertyName("price_change_percentage_24h")]
    public decimal PriceChange { get; set; }
    public DateTime DateTime { get; set; } = DateTime.Now;
    
    public int Crypto_Id { get; set; } // Klucz obcy
}