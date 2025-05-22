using Conflux.Data;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class ProductsServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private ConfluxContext _context = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        ConfluxContext context = new(options);
        await context.Database.EnsureCreatedAsync();
        _context = context;
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnProduct_WhenProductExists()
    {
        // Arrange
        ProductsService productsService = new(_context);

        // Add product to the context
        Guid productId = Guid.NewGuid();
        Product testProduct = new()
        {
            Id = productId,
            Schema = ProductSchema.Doi,
            Url = "https://doi.org/product",
            Title = "Test Product",
            Type = ProductType.Dataset,
            Categories =
            [
                ProductCategoryType.Output,
            ],
        };

        _context.Products.Add(testProduct);
        await _context.SaveChangesAsync();

        // Act
        ProductResponseDTO product = await productsService.GetProductByIdAsync(productId);

        // Assert
        Assert.NotNull(product);
        Assert.Equal(testProduct.Id, product.Id);
        Assert.Equal(testProduct.Title, product.Title);
        Assert.Equal(testProduct.Schema, product.Schema);
        Assert.Equal(testProduct.Url, product.Url);
        Assert.Equal(testProduct.Type, product.Type);
        Assert.Single(product.Categories);
        Assert.Contains(product.Categories, c => c == ProductCategoryType.Output);
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldThrowException_WhenProductDoesNotExist()
    {
        // Arrange
        ProductsService productsService = new(_context);
        Guid nonExistentProductId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            productsService.GetProductByIdAsync(nonExistentProductId));
    }

    [Fact]
    public async Task CreateProductAsync_ShouldCreateProduct()
    {
        // Arrange
        ProductsService productsService = new(_context);

        // Create a DTO with empty Categories - they'll be populated by the service
        ProductRequestDTO dto = new()
        {
            Schema = ProductSchema.Doi,
            Url = "https://doi.org/newproduct",
            Title = "New Test Product",
            Type = ProductType.Dataset,
            Categories = [ProductCategoryType.Output, ProductCategoryType.Input],
        };

        // Act
        ProductResponseDTO createdProduct = await productsService.CreateProductAsync(dto);

        // Assert
        Assert.NotNull(createdProduct);
        Assert.NotEqual(Guid.Empty, createdProduct.Id);
        Assert.Equal(dto.Title, createdProduct.Title);
        Assert.Equal(dto.Schema, createdProduct.Schema);
        Assert.Equal(dto.Url, createdProduct.Url);
        Assert.Equal(dto.Type, createdProduct.Type);

        // Verify product is in the database
        Product? storedProduct = await _context.Products.FindAsync(createdProduct.Id);
        Assert.NotNull(storedProduct);
        Assert.Equal(dto.Title, storedProduct.Title);
        Assert.Equal(2, storedProduct.Categories.Count);
        Assert.Contains(storedProduct.Categories, c => c == ProductCategoryType.Output);
        Assert.Contains(storedProduct.Categories, c => c == ProductCategoryType.Input);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldUpdateProduct()
    {
        // Arrange
        ProductsService productsService = new(_context);

        // Add an initial product to the context
        Guid productId = Guid.NewGuid();
        Product initialProduct = new()
        {
            Id = productId,
            Schema = ProductSchema.Doi,
            Url = "https://doi.org/initial",
            Title = "Initial Product",
            Type = ProductType.Software,
            Categories =
            [
                ProductCategoryType.Output,
            ],
        };
        _context.Products.Add(initialProduct);
        await _context.SaveChangesAsync();

        // Detach the initial product to avoid tracking issues if ProductsService loads it again
        _context.Entry(initialProduct).State = EntityState.Detached;

        ProductRequestDTO updatedProductDto = new()
        {
            Schema = ProductSchema.Doi,
            Url = "https://doi.org/updated-product",
            Title = "Updated Test Product",
            Type = ProductType.Service,
            Categories =
            [
                ProductCategoryType.Input,
                ProductCategoryType.Internal,
            ],
        };

        // Act
        ProductResponseDTO updatedProductResponse = await productsService.UpdateProductAsync(productId, updatedProductDto);

        // Assert
        Assert.NotNull(updatedProductResponse);
        Assert.Equal(productId, updatedProductResponse.Id);
        Assert.Equal(updatedProductDto.Title, updatedProductResponse.Title);
        Assert.Equal(updatedProductDto.Schema, updatedProductResponse.Schema);
        Assert.Equal(updatedProductDto.Url, updatedProductResponse.Url);
        Assert.Equal(updatedProductDto.Type, updatedProductResponse.Type);
        Assert.Equal(2, updatedProductResponse.Categories.Count);
        Assert.Contains(ProductCategoryType.Input, updatedProductResponse.Categories);
        Assert.Contains(ProductCategoryType.Internal, updatedProductResponse.Categories);

        // Verify product is updated in the database
        Product? storedProduct = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId);

        Assert.NotNull(storedProduct);
        Assert.Equal(updatedProductDto.Title, storedProduct.Title);
        Assert.Equal(updatedProductDto.Schema, storedProduct.Schema);
        Assert.Equal(updatedProductDto.Url, storedProduct.Url);
        Assert.Equal(updatedProductDto.Type, storedProduct.Type);
        Assert.Equal(2, storedProduct.Categories.Count);
        Assert.Contains(ProductCategoryType.Input, storedProduct.Categories);
        Assert.Contains(ProductCategoryType.Internal, storedProduct.Categories);
        Assert.DoesNotContain(ProductCategoryType.Output, storedProduct.Categories);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldThrowException_WhenProductDoesNotExist()
    {
        // Arrange
        ProductsService productsService = new(_context);
        Guid nonExistentProductId = Guid.NewGuid();

        ProductRequestDTO updateDto = new()
        {
            Schema = ProductSchema.Doi,
            Url = "https://doi.org/updated-product",
            Title = "Updated Test Product",
            Type = ProductType.Service,
            Categories =
            [
                ProductCategoryType.Input,
            ],
        };

        // Act & Assert
        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            productsService.UpdateProductAsync(nonExistentProductId, updateDto));
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldDeleteProduct()
    {
        // Arrange
        ProductsService productsService = new(_context);

        // Add product to the context
        Guid productId = Guid.NewGuid();
        Product testProduct = new()
        {
            Id = productId,
            Schema = ProductSchema.Doi,
            Url = "https://doi.org/product",
            Title = "Test Product",
            Type = ProductType.Dataset,
            Categories =
            [
                ProductCategoryType.Output,
            ],
        };

        _context.Products.Add(testProduct);
        await _context.SaveChangesAsync();

        // Act
        await productsService.DeleteProductAsync(productId);

        // Assert
        Product? deletedProduct = await _context.Products.FindAsync(productId);
        Assert.Null(deletedProduct);
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldThrowException_WhenProductDoesNotExist()
    {
        // Arrange
        ProductsService productsService = new(_context);
        Guid nonExistentProductId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            productsService.DeleteProductAsync(nonExistentProductId));
    }
}
