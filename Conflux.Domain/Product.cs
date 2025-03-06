using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain;

/// <summary>
/// Represents a product.
/// </summary>
public class Product
{
    [Key]
    public string Url { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Title { get; set; }
}
