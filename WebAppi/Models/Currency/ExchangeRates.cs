namespace WebAppi.Models;

public class ExchangeRates
{
    public int Id { get; set; } // Klucz główny
    public int Id2 { get; set; } // Drugi klucz obcy
    public string ExchangeRate { get; set; } // Wartość kursu wymiany
}