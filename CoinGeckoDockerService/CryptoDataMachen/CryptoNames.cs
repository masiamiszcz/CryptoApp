using System.Text.Json.Serialization;

namespace CoinGeckoDockerService;

public class CryptoNames
{
    public int Id { get; set; }
    
    public string CryptoName { get; set; }
    
    public string Symbol { get; set; }
    
    public string Image { get; set; }
}