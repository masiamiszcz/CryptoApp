using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppi.Models;

public class CryptoModel
{
    public decimal High24 { get; set; } // Najwyższa cena w ciągu 24h
    public decimal Low24 { get; set; }  // Najniższa cena w ciągu 24h
    public decimal CryptoPrice { get; set; } // Aktualna cena
    public decimal PriceChange { get; set; } // Zmiana w ciągu 24h

    [Key]
    public DateTime DateTime { get; set; } // Klucz główny — czas rejestracji

    // Klucz obcy do tabeli CryptoNames
    [ForeignKey("Crypto_Id")]
    public int Crypto_Id { get; set; }

    public CryptoNamesModel CryptoNameNavigation { get; set; } // Nawigacja do CryptoNames
}