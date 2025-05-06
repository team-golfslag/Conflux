// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs.Patch;

public class ProductPatchDTO
{
    public ProductSchema? Schema { get; init; }
    public string? Url { get; init; }
    public string? Title { get; init; }
    public HashSet<ProductCategoryType>? Categories { get; init; }
}
