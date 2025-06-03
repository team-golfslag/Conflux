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

/// <summary>
/// The ProductsService class provides methods for managing and performing CRUD operations on products
/// within the context of a specific project.
/// </summary>
public class ProductsService : IProductsService
{
    private readonly ConfluxContext _context;

    public ProductsService(ConfluxContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves a product by its ID within the specified project.
    /// </summary>
    /// <param name="projectId">The ID of the project the product belongs to.</param>
    /// <param name="productId">The ID of the product to retrieve.</param>
    /// <returns>A <see cref="ProductResponseDTO" /> representing the product.</returns>
    /// <exception cref="ProductNotFoundException">Thrown if the product does not exist.</exception>
    public async Task<ProductResponseDTO> GetProductByIdAsync(Guid projectId, Guid productId)
    {
        Product product = await GetProductEntityAsync(projectId, productId);
        if (product.ProjectId != projectId) throw new ProductNotFoundException(projectId, productId);
        return MapToProductResponseDTO(projectId, product);
    }

    /// <summary>
    /// Creates a new product within the specified project.
    /// </summary>
    /// <param name="projectId">The ID of the project to add the product to.</param>
    /// <param name="productDTO">The product data to create.</param>
    /// <returns>A <see cref="ProductResponseDTO" /> representing the created product.</returns>
    public async Task<ProductResponseDTO> CreateProductAsync(Guid projectId, ProductRequestDTO productDTO)
    {
        Project project = await _context.Projects.FindAsync(projectId) ?? throw new ProjectNotFoundException(projectId);
        Product product = new()
        {
            ProjectId = projectId,
            Id = Guid.NewGuid(),
            Schema = productDTO.Schema,
            Url = productDTO.Url,
            Title = productDTO.Title,
            Type = productDTO.Type,
            Categories = productDTO.Categories,
        };

        await _context.Products.AddAsync(product);
        // add the product to the project
        project.Products.Add(product);

        await _context.SaveChangesAsync();

        return MapToProductResponseDTO(projectId, product);
    }

    /// <summary>
    /// Updates an existing product within the specified project.
    /// </summary>
    /// <param name="projectId">The ID of the project the product belongs to.</param>
    /// <param name="productId">The ID of the product to update.</param>
    /// <param name="productDTO">The updated product data.</param>
    /// <returns>A <see cref="ProductResponseDTO" /> representing the updated product.</returns>
    /// <exception cref="ProductNotFoundException">Thrown if the product does not exist.</exception>
    public async Task<ProductResponseDTO> UpdateProductAsync(Guid projectId, Guid productId,
        ProductRequestDTO productDTO)
    {
        Product product = await GetProductEntityAsync(projectId, productId);
        if (product.ProjectId != projectId) throw new ProductNotFoundException(projectId, productId);

        product.Schema = productDTO.Schema;
        product.Url = productDTO.Url;
        product.Title = productDTO.Title;
        product.Type = productDTO.Type;
        product.Categories = productDTO.Categories;

        _context.Products.Update(product);
        await _context.SaveChangesAsync();

        return MapToProductResponseDTO(projectId, product);
    }

    /// <summary>
    /// Deletes a product by its ID within the specified project.
    /// </summary>
    /// <param name="projectId">The ID of the project the product belongs to.</param>
    /// <param name="productId">The ID of the product to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ProductNotFoundException">Thrown if the product does not exist.</exception>
    public async Task DeleteProductAsync(Guid projectId, Guid productId)
    {
        Product product = await GetProductEntityAsync(projectId, productId);
        if (product.ProjectId != projectId) throw new ProductNotFoundException(projectId, productId);

        // First load the project with its products collection
        Project project = await _context.Projects
                .Include(p => p.Products)
                .SingleOrDefaultAsync(p => p.Id == projectId)
            ?? throw new ProjectNotFoundException(projectId);

        // Remove the product from the project's collection
        project.Products.Remove(product);

        // Then remove the product itself
        _context.Products.Remove(product);

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Retrieves a product entity by its ID.
    /// </summary>
    /// <param name="projectId">The ID of the related <see cref="Project" /></param>
    /// <param name="productId">The ID of the product to retrieve.</param>
    /// <returns>The <see cref="Product" /> entity.</returns>
    /// <exception cref="ProductNotFoundException">Thrown if the product does not exist.</exception>
    private async Task<Product> GetProductEntityAsync(Guid projectId, Guid productId) =>
        await _context.Products.SingleOrDefaultAsync(p => p.Id == productId) ??
        throw new ProductNotFoundException(projectId, productId);

    /// <summary>
    /// Maps the given <see cref="Product" /> to a <see cref="ProductResponseDTO" />.
    /// </summary>
    /// <param name="projectId">The ID for the related <see cref="Project" /></param>
    /// <param name="product">The product entity to be mapped.</param>
    /// <returns>A <see cref="ProductResponseDTO" /> instance containing the mapped product data.</returns>
    private ProductResponseDTO MapToProductResponseDTO(Guid projectId, Product product) =>
        new()
        {
            ProjectId = projectId,
            Id = product.Id,
            Schema = product.Schema,
            Url = product.Url,
            Title = product.Title,
            Type = product.Type,
            Categories = product.Categories,
        };
}
