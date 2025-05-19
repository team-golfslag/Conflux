// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;

namespace Conflux.Domain.Logic.Services;

public interface IProductsService
{
    public Task<List<ProductResponseDTO>> GetProductsByQueryAsync(string? query);
    public Task<ProductResponseDTO> GetProductByIdAsync(Guid productId);
    public Task<ProductResponseDTO> CreateProductAsync(ProductRequestDTO productDTO);
    public Task<ProductResponseDTO> UpdateProductAsync(Guid productId,
        ProductRequestDTO productDTO);
    public Task DeleteProductAsync(Guid productId);
}
