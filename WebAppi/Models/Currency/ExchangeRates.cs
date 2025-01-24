using System.ComponentModel.DataAnnotations;

namespace WebAppi.Models;

public class ExchangeRates
{

    public int Id { get; set; }


    public int Id2 { get; set; }
    

    public int ApiId { get; set; }
    public string ExchangeRate { get; set; }
    
    [Key]
    public DateTime DateTime { get; set; } = DateTime.Now;
    
    public CurrencyNames CurrencyNameNavigation { get; set; }
    public CurrencyNames CurrencyNameNavigation2 { get; set; }
}