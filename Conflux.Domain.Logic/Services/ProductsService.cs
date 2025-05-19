// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Domain.Logic.Services;

public class ProductsService : IProductsService
{
    private readonly ConfluxContext _context;

    public ProductsService(ConfluxContext context)
    {
        _context = context;
    }

    public async Task<List<ProductResponseDTO>> GetProductsByQueryAsync(string? query)
    {
        List<Product> products = await _context.Products
            .Where(p => string.IsNullOrEmpty(query) || p.Title.Contains(query))
            .ToListAsync();

        return products.Select(MapToProductResponseDTO).ToList();
    }

    public async Task<ProductResponseDTO> GetProductByIdAsync(Guid productId)
    {
        Product product = await GetProductEntityAsync(productId);
        return MapToProductResponseDTO(product);
    }

    public async Task<ProductResponseDTO> CreateProductAsync(ProductRequestDTO productDTO)
    {
       Product product = new()
        {
            Id = Guid.NewGuid(),
            Schema = productDTO.Schema,
            Url = productDTO.Url,
            Title = productDTO.Title,
            Type = productDTO.Type,
            Categories = productDTO.Categories,
        };

        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        return MapToProductResponseDTO(product);
    }

    public async Task<ProductResponseDTO> UpdateProductAsync(Guid productId, ProductRequestDTO productDTO)
    {
        Product product = await GetProductEntityAsync(productId);

        product.Schema = productDTO.Schema;
        product.Url = productDTO.Url;
        product.Title = productDTO.Title;
        product.Type = productDTO.Type;
        product.Categories = productDTO.Categories;

        _context.Products.Update(product);
        await _context.SaveChangesAsync();

        return MapToProductResponseDTO(product);
    }

    public async Task DeleteProductAsync(Guid productId)
    {
        Product product = await GetProductEntityAsync(productId);
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
    }

    private async Task<Product> GetProductEntityAsync(Guid productId) =>
        await _context.Products.SingleOrDefaultAsync(p => p.Id == productId) ??
        throw new ProductNotFoundException(productId);

    /// <summary>
    /// Maps the given <see cref="Product" /> to a <see cref="ProductResponseDTO" />.
    /// </summary>
    /// <param name="product">The product entity to be mapped.</param>
    /// <returns>A <see cref="ProductResponseDTO" /> instance containing the mapped product data.</returns>
    private ProductResponseDTO MapToProductResponseDTO(Product product) =>
        new()
        {
            Id = product.Id,
            Schema = product.Schema,
            Url = product.Url,
            Title = product.Title,
            Type = product.Type,
            Categories = product.Categories,
        };
}
