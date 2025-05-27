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

    [Key]
    public Guid Id { get; init; }

    public Guid ProjectId { get; init; }
    public Project? Project { get; init; }

    public ProductSchema Schema { get; set; }

    // TODO: Kijk of de identifier wel echt aan het schema voldoet.
    public string Url { get; set; }

    public required string Title { get; set; }

    public required ProductType Type { get; init; }
    public string TypeSchemaUri => "https://vocabulary.raid.org/relatedObject.type.schema/329";

    /// <summary>Fully-qualified URI for the selected <see cref="Type" />.</summary>
    public string GetTypeUri => $"https://vocabulary.raid.org/relatedObject.type.schema/{(int)Type}";

    public string? SchemaUri =>
        Schema switch
        {
            ProductSchema.Ark     => "https://arks.org/",
            ProductSchema.Doi     => "http://doi.org/",
            ProductSchema.Handle  => "http://hdl.handle.net/",
            ProductSchema.Isbn    => "https://www.isbn-international.org/",
            ProductSchema.Rrid    => "https://scicrunch.org/resolver/",
            ProductSchema.Archive => "https://archive.org/",
            _                     => throw new ArgumentOutOfRangeException(),
        };

    public HashSet<ProductCategory> Categories { get; set; } = [];
}
