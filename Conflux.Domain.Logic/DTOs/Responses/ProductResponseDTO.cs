// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs.Responses;

public class ProductResponseDTO
{
    public Guid ProjectId { get; init; }
    public Guid Id { get; init; }

    public ProductSchema Schema { get; init; }

    public string Url { get; init; }

    public required string Title { get; init; }

    public required ProductType? Type { get; init; }

    public List<ProductCategoryType> Categories { get; init; } = [];
}
