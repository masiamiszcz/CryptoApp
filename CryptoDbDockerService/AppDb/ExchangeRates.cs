using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoDbDockerService.AppDb;

public class ExchangeRates
{

    public int Id { get; set; }


    public int Id2 { get; set; }
    

    public int ApiId { get; set; }
    public string ExchangeRate { get; set; }
    
    [Key]
    public DateTime DateTime { get; set; } = DateTime.Now;
}
