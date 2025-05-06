// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic;

public class ProductDTO
{
    public Guid Id { get; init; }
    public ProductSchema? Schema { get; init; }
    public string? Url { get; init; }
    public required string Title { get; init; }
    public required ProductType Type { get; init; }
    public HashSet<ProductCategoryType> Categories { get; init; } = [];

    public Product ToProduct() =>
        new()
        {
            Schema = Schema,
            Url = Url,
            Title = Title,
            Type = Type,
            Categories = Categories.ToList().ConvertAll(c => new ProductCategory
            {
                Type = c,
                ProductId = Id,
            }).ToHashSet(),
        };
}
