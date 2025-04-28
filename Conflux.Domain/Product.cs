// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain;

/// <summary>
/// Represents a product.
/// </summary>
public class Product
{
    /// <summary>
    /// Base URI for this controlled list (matches RAiD vocabulary “relatedObject.type.schema”).
    /// </summary>
    public const string TitleTypeSchemaUri = "https://vocabulary.raid.org/relatedObject.type.schema/329";

    [Key] public Guid Id { get; init; }

    public string? Url { get; set; }

    public required string Title { get; set; }

    public required ProductType Type { get; init; }

    /// <summary>Fully-qualified URI for the selected <see cref="Type" />.</summary>
    public string GetTitleTypeUri => $"https://vocabulary.raid.org/relatedObject.type.schema/{(int)Type}";

    public HashSet<ProductCategory> Categories { get; set; } = [];
}
