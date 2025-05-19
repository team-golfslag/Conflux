// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs.Responses;

public class ProductResponseDTO
{
    public Guid Id { get; init; }

    public ProductSchema Schema { get; set; }

    public string Url { get; set; }

    public required string Title { get; set; }

    public required ProductType Type { get; init; }
    
    public HashSet<ProductCategory> Categories { get; set; } = [];
}
