using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain;

/// <summary>
/// Represents a product.
/// </summary>
public class Product
{
    [Key] public string Url { get; set; }

    public required string Title { get; set; }
}
