using System.ComponentModel.DataAnnotations;

namespace WebAppi.Models;

public class CryptoNamesModel
{
    [Key]
    public int Id { get; set; } // Klucz główny

    public string CryptoName { get; set; } // Nazwa kryptowaluty
    public string Symbol { get; set; } // Symbol kryptowaluty (np. BTC, ETH)
    public string Image { get; set; }
}