using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CurrencyService.CurrencyModels;

public class ExchangeRates
{
    [NotMapped]
    public string CurrencyName { get; set; }
    
    [NotMapped]
    public string Symbol { get; set; }
    
    [Key, Column(Order = 0)]
    
    public int Id { get; set; }

    [Key, Column(Order = 1)]
    public int Id2 { get; set; }

    public string ExchangeRate { get; set; }
    
    public DateTime DateTime { get; set; } = DateTime.Now;
    
    public int ApiId { get; set; }
}