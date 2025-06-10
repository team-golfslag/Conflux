// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.API.Controllers;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Conflux.API.Tests.Controllers;

public class ProductsControllerTests 
{
    private readonly ProductsController _controller;
    private readonly Mock<IProductsService> _mockService;

    public ProductsControllerTests()
    {
        _mockService = new();
        _controller = new(_mockService.Object);
    }

    [Fact]
    public async Task GetProductById_ReturnsSuccess_ForValidId()
    {
        // Arrange
        Guid projectId = Guid.CreateVersion7();
        Guid productId = Guid.CreateVersion7();

        ProductResponseDTO dto = new()
        {
            Id = productId,
            ProjectId = projectId,
            Title = "Test Product",
            Schema = ProductSchema.Doi,
            Url = "https://doi.org/testproduct",
            Type = ProductType.Dataset,
            Categories = [ProductCategoryType.Output],
        };

        _mockService.Setup(s => s.GetProductByIdAsync(projectId, productId)).ReturnsAsync(dto);

        // Act
        ActionResult<ProductResponseDTO> response = await _controller.GetProductByIdAsync(projectId, productId);

        // Assert
        ProductResponseDTO? productResponse = response.Value;
        Assert.NotNull(productResponse);
        Assert.Equal(productId, productResponse.Id);
        Assert.Equal(dto.Title, productResponse.Title);
        Assert.Equal(dto.Schema, productResponse.Schema);
        Assert.Equal(dto.Url, productResponse.Url);
        Assert.Equal(dto.Type, productResponse.Type);
        Assert.Equal(dto.Categories.Count, productResponse.Categories.Count);
        Assert.Contains(ProductCategoryType.Output, productResponse.Categories);
    }

    [Fact]
    public async Task GetProductById_ReturnsNotFound_ForInvalidId()
    {
        // Arrange
        Guid invalidId = Guid.CreateVersion7();
        Guid projectId = Guid.CreateVersion7();

        _mockService.Setup(s => s.GetProductByIdAsync(projectId, invalidId))
            .ThrowsAsync(new ProductNotFoundException(projectId, invalidId));

        // Act and Assert
        await Assert.ThrowsAsync<ProductNotFoundException>(() => _controller.GetProductByIdAsync(projectId, invalidId));
    }

    [Fact]
    public async Task CreateProduct_ReturnsCreatedProduct()
    {
        // Arrange
        Guid projectId = Guid.CreateVersion7();
        ProductResponseDTO createdProduct = new()
        {
            Id = Guid.CreateVersion7(),
            ProjectId = projectId,
            Title = "New Test Product",
            Schema = ProductSchema.Doi,
            Url = "https://doi.org/newproduct",
            Type = ProductType.Dataset,
            Categories = [ProductCategoryType.Output, ProductCategoryType.Internal],
        };

        ProductRequestDTO dto = new()
        {
            Schema = ProductSchema.Doi,
            Url = "https://doi.org/newproduct",
            Title = "New Test Product",
            Type = ProductType.Dataset,
            Categories = [ProductCategoryType.Output, ProductCategoryType.Internal],
        };

        _mockService.Setup(s => s.CreateProductAsync(projectId, It.IsAny<ProductRequestDTO>()))
            .ReturnsAsync(createdProduct);

        // Act
        ActionResult<ProductResponseDTO> response = await _controller.CreateProductAsync(projectId, dto);

        // Assert
        ProductResponseDTO? product = response.Value;
        Assert.NotNull(product);
        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal(dto.Title, product.Title);
        Assert.Equal(dto.Schema, product.Schema);
        Assert.Equal(dto.Url, product.Url);
        Assert.Equal(dto.Type, product.Type);
        Assert.Equal(dto.Categories.Count, product.Categories.Count);
        Assert.Contains(ProductCategoryType.Output, product.Categories);
        Assert.Contains(ProductCategoryType.Internal, product.Categories);
    }

    [Fact]
    public async Task UpdateProduct_UpdatesProduct()
    {
        // Arrange
        Guid projectId = Guid.CreateVersion7();
        Guid productId = Guid.CreateVersion7();

        ProductRequestDTO dto = new()
        {
            Schema = ProductSchema.Doi,
            Url = "https://doi.org/testupdateproduct",
            Title = "Original Test Product",
            Type = ProductType.Dataset,
            Categories = [ProductCategoryType.Input, ProductCategoryType.Internal],
        };

        ProductResponseDTO updatedProduct = new()
        {
            Id = productId,
            ProjectId = projectId,
            Title = dto.Title,
            Schema = dto.Schema,
            Url = dto.Url,
            Type = dto.Type,
            Categories = dto.Categories,
        };

        _mockService.Setup(s => s.UpdateProductAsync(projectId, productId, It.IsAny<ProductRequestDTO>()))
            .ReturnsAsync(updatedProduct);

        // Act
        ActionResult<ProductResponseDTO> updateResponse = await _controller.UpdateProductAsync(projectId, productId, dto);

        // Assert
        ProductResponseDTO? updatedProductResponse = updateResponse.Value;
        Assert.NotNull(updatedProductResponse);
        Assert.Equal(updatedProduct.Id, updatedProductResponse.Id);
        Assert.Equal(updatedProduct.Title, updatedProductResponse.Title);
        Assert.Equal(updatedProduct.Schema, updatedProductResponse.Schema);
        Assert.Equal(updatedProduct.Url, updatedProductResponse.Url);
        Assert.Equal(updatedProduct.Type, updatedProductResponse.Type);
        Assert.Equal(updatedProduct.Categories.Count, updatedProductResponse.Categories.Count);
        Assert.Contains(ProductCategoryType.Input, updatedProductResponse.Categories);
        Assert.Contains(ProductCategoryType.Internal, updatedProductResponse.Categories);
    }

    [Fact]
    public async Task UpdateProduct_ReturnsNotFound_ForInvalidId()
    {
        // Arrange
        Guid projectId = Guid.CreateVersion7();
        Guid productId = Guid.CreateVersion7();
        ProductRequestDTO updateDto = new()
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

        _mockService.Setup(s => s.UpdateProductAsync(projectId, productId, It.IsAny<ProductRequestDTO>()))
            .ThrowsAsync(new ProductNotFoundException(projectId, productId));

        // Act and Assert
        await Assert.ThrowsAsync<ProductNotFoundException>(() => _controller.UpdateProductAsync(projectId, productId, updateDto));
    }

    [Fact]
    public async Task DeleteProduct_DeletesProduct()
    {
        // Arrange
        Guid projectId = Guid.CreateVersion7();
        Guid productId = Guid.CreateVersion7();

        _mockService.Setup(s => s.DeleteProductAsync(projectId, productId)).Returns(Task.CompletedTask);

        // Act
        ActionResult response = await _controller.DeleteProductAsync(projectId, productId);
        
        // Assert
        Assert.IsType<OkResult>(response);
    }

    [Fact]
    public async Task DeleteProduct_ReturnsNotFound_ForInvalidId()
    {
        // Arrange
        Guid projectId = Guid.CreateVersion7();
        Guid productId = Guid.CreateVersion7();
        
        _mockService.Setup(s => s.DeleteProductAsync(projectId, productId))
            .ThrowsAsync(new ProductNotFoundException(projectId, productId));
        
        // Act and Assert
        ActionResult response = await _controller.DeleteProductAsync(projectId, productId);
        Assert.IsType<NotFoundObjectResult>(response);
    }
}
