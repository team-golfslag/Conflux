using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain;

public class Product
{
    [Key]
    public string Url { get; set; }
    public string Title { get; set; }
}
