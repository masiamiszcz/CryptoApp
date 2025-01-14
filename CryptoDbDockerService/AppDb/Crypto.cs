

using System.ComponentModel.DataAnnotations;

public class Crypto
{
    public decimal High24 { get; set; }
    public decimal Low24 { get; set; }
    
    public int Crypto_Id { get; set; }
    
    public decimal CryptoPrice { get; set; }
    public decimal PriceChange { get; set; }
    
    [Key]
    public DateTime DateTime { get; set; } = DateTime.Now;
}
