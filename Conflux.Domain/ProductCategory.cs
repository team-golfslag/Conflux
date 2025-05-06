// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Domain;

public enum ProductCategoryType
{
    Input = 191,
    Internal = 192,
    Output = 190,
}

[PrimaryKey(nameof(ProductId), nameof(Type))]
public class ProductCategory
{
    [ForeignKey(nameof(Product))] public Guid ProductId { get; init; }
    public ProductCategoryType Type { get; init; }

    public string SchemaUri => "https://vocabulary.raid.org/relatedObject.category.schema/385";

    public string GetUri => $"https://vocabulary.raid.org/relatedObject.category.id/{(int)Type}";
}
