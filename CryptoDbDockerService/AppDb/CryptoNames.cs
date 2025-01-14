using System.ComponentModel.DataAnnotations;

namespace CryptoDbDockerService.AppDb;

public class CryptoNames
{
    [Key]
    public int Id { get; set; }
    public string CryptoName { get; set; }
    public string Symbol { get; set; }
    public string Image { get; set; }
}