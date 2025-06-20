// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Conflux.Integrations.Archive;
using Crossref.Net.Models;
using Crossref.Net.Services;
using DoiTools.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class ProductsServiceTests : IDisposable
{
    private readonly ConfluxContext _context = null!;
    private readonly Mock<ICrossrefService> _mockCrossrefService = new();
    private readonly Mock<IWebArchiveService> _mockWebArchiveService = new();

    private readonly Project _project;

    public ProductsServiceTests()
    {
        ServiceProvider serviceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.CreateVersion7()}")
            .UseInternalServiceProvider(serviceProvider)
            .Options;

        ConfluxContext context = new(options);
        context.Database.EnsureCreated();
        _context = context;

        // Seed a sample project
        _project = new()
        {
            Id = Guid.CreateVersion7(),
            Titles =
            [
                new()
                {
                    Id = Guid.CreateVersion7(),
                    ProjectId = Guid.CreateVersion7(),
                    Text = "Sample Project",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                },
            ],
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
        };

        _context.Projects.Add(_project);
        
        _mockCrossrefService.Setup(s => s.GetWorkAsync(It.IsAny<Doi>()))
            .ReturnsAsync(new Work
            {
                Type = "journal-article",
                Title = ["Sample Title"],
            });
        
        _mockWebArchiveService.Setup(s => s.CreateArchiveLinkAsync(It.IsAny<string>()))
            .ReturnsAsync("https://web.archive.org/web/20231001000000/https://doi.org/sample-doi");

    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnProduct_WhenProductExists()
    {
        // Arrange
        ProductsService productsService = new(_context, _mockCrossrefService.Object, _mockWebArchiveService.Object);

        // Add product to the context
        Guid projectId = _project.Id;
        Guid productId = Guid.CreateVersion7();
        Product testProduct = new()
        {
            ProjectId = projectId,
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
        ProductResponseDTO product = await productsService.GetProductByIdAsync(projectId, productId);

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
        ProductsService productsService = new(_context, _mockCrossrefService.Object, _mockWebArchiveService.Object);
        Guid projectId = _project.Id;
        Guid nonExistentProductId = Guid.CreateVersion7();

        // Act & Assert
        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            productsService.GetProductByIdAsync(projectId, nonExistentProductId));
    }

    [Fact]
    public async Task CreateProductAsync_ShouldCreateProduct()
    {
        // Arrange
        ProductsService productsService = new(_context, _mockCrossrefService.Object, _mockWebArchiveService.Object);

        ProductRequestDTO dto = new()
        {
            Schema = ProductSchema.Doi,
            Url = "https://doi.org/newproduct",
            Title = "New Test Product",
            Type = ProductType.Dataset,
            Categories = [ProductCategoryType.Output, ProductCategoryType.Input],
        };

        // Act
        ProductResponseDTO createdProduct = await productsService.CreateProductAsync(_project.Id, dto);

        // Assert
        Assert.NotNull(createdProduct);
        Assert.NotEqual(Guid.Empty, createdProduct.Id);
        Assert.Equal(dto.Title, createdProduct.Title);
        Assert.Equal(dto.Schema, createdProduct.Schema);
        Assert.Equal(dto.Url, createdProduct.Url);
        Assert.Equal(dto.Type, createdProduct.Type);

        // Verify product is in the database in the related project
        Project? storedProject = await _context.Projects
            .Include(p => p.Products)
            .FirstOrDefaultAsync(p => p.Id == _project.Id);

        Product? storedProduct = storedProject?.Products.Find(p => p.Id == createdProduct.Id);
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
        ProductsService productsService = new(_context, _mockCrossrefService.Object, _mockWebArchiveService.Object);

        // Add project to the context
        Guid projectId = _project.Id;

        // Add an initial product to the context
        Guid productId = Guid.CreateVersion7();
        Product initialProduct = new()
        {
            ProjectId = projectId,
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
        _project.Products.Add(initialProduct);
        _context.Projects.Add(_project);
        await _context.SaveChangesAsync();

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
        ProductResponseDTO updatedProductResponse =
            await productsService.UpdateProductAsync(projectId, productId, updatedProductDto);

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
        Project? storedProject = await _context.Projects
            .Include(p => p.Products)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        Product? storedProduct = storedProject?.Products.Find(p => p.Id == updatedProductResponse.Id);

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
        ProductsService productsService = new(_context, _mockCrossrefService.Object, _mockWebArchiveService.Object);
        Guid projectId = _project.Id;
        Guid nonExistentProductId = Guid.CreateVersion7();

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
            productsService.UpdateProductAsync(projectId, nonExistentProductId, updateDto));
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldDeleteProduct()
    {
        // Arrange
        ProductsService productsService = new(_context, _mockCrossrefService.Object, _mockWebArchiveService.Object);

        Guid projectId = _project.Id;

        // Add product to the context
        Guid productId = Guid.CreateVersion7();
        Product testProduct = new()
        {
            ProjectId = projectId,
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
        await productsService.DeleteProductAsync(projectId, productId);

        // Assert
        Product? deletedProduct = await _context.Products.FindAsync(productId);
        Assert.Null(deletedProduct);
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldThrowException_WhenProductDoesNotExist()
    {
        // Arrange
        ProductsService productsService = new(_context, _mockCrossrefService.Object, _mockWebArchiveService.Object);
        Guid projectId = Guid.CreateVersion7();
        Guid nonExistentProductId = Guid.CreateVersion7();

        // Act & Assert
        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            productsService.DeleteProductAsync(projectId, nonExistentProductId));
    }
    
    [Fact]
    public async Task GetInfoFromDoi_ShouldReturnProduct_WhenDoiExists()
    {
        // Arrange
        ProductsService productsService = new(_context, _mockCrossrefService.Object, _mockWebArchiveService.Object);
        string testDoi = "10.1234/test-doi";

        // Act
        ProductResponseDTO product = await productsService.GetInfoFromDoi(testDoi);

        // Assert
        Assert.NotNull(product);
        Assert.Equal("Sample Title", product.Title);
        Assert.Equal(ProductSchema.Doi, product.Schema);
        Assert.Equal(ProductType.JournalArticle, product.Type);
    }
    
    [Fact]
    public async Task GetArchiveLinkForUrl_ShouldReturnArchivedUrl_WhenUrlExists()
    {
        // Arrange
        ProductsService productsService = new(_context, _mockCrossrefService.Object, _mockWebArchiveService.Object);
        string testUrl = "https://doi.org/sample-doi";

        // Act
        string archiveUrl = await productsService.GetArchiveLinkForUrl(testUrl);

        // Assert
        Assert.Equal("https://web.archive.org/web/20231001000000/https://doi.org/sample-doi", archiveUrl);
    }
}
