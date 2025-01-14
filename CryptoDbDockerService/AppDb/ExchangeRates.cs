using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoDbDockerService.AppDb;

public class ExchangeRates
{
    [Key, Column(Order = 0)]
    public int Id { get; set; }

    [Key, Column(Order = 1)]
    public int Id2 { get; set; }
    public string ExchangeRate { get; set; }
    
    public DateTime Date { get; set; } = DateTime.Now;
    
    public int ApiId { get; set; }
}