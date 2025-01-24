using System.ComponentModel.DataAnnotations;

namespace CurrencyService.CurrencyModels;

public class CurrencyNames
{
    [Key]
    public int Id { get; set; }
    
    public string? CurrencyName { get; set; }

    public string Symbol { get; set; }
}