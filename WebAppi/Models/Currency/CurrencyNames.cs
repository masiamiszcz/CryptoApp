namespace WebAppi.Models;

public class CurrencyNames
{
    public int Id { get; set; } // Klucz główny
    public string CurrencyName { get; set; } // Nazwa waluty (np. Dolar, Euro)
    public string Symbol { get; set; } // Symbol waluty (np. USD, EUR)
}