// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;

namespace Conflux.Domain.Logic.Services;

public interface IProductsService
{
    public Task<ProductResponseDTO> GetProductByIdAsync(Guid projectId, Guid productId);
    public Task<ProductResponseDTO> CreateProductAsync(Guid projectId, ProductRequestDTO productDTO);

    public Task<ProductResponseDTO> UpdateProductAsync(Guid projectId, Guid productId,
        ProductRequestDTO productDTO);

    public Task DeleteProductAsync(Guid projectId, Guid productId);
    public Task<ProductResponseDTO> GetInfoFromDoi(string doi);
    public Task<ProductResponseDTO> GetArchiveLinkForUrl(string url);
}
