using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CurrencyService.CurrencyModels;

public class ExchangeRates
{
    public int Id { get; set; }

 
    public int Id2 { get; set; }
    
    
    public int ApiId { get; set; }
    public decimal ExchangeRate { get; set; }
    [Key]
    public DateTime DateTime { get; set; } = DateTime.Now;
    
}