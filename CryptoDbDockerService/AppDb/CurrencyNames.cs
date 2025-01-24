using System.ComponentModel.DataAnnotations;

namespace CryptoDbDockerService.AppDb;

public class CurrencyNames
{
    [Key]
    public int Id { get; set; }
    public string? CurrencyName { get; set; }
    public string Symbol { get; set; }
}