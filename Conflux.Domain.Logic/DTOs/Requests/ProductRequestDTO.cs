// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs.Requests;

public class ProductRequestDTO
{
    public required ProductSchema Schema { get; init; }
    public required string Url { get; init; }
    public required string Title { get; init; }
    public required ProductType Type { get; init; }
    public HashSet<ProductCategory> Categories { get; init; } = [];
}
