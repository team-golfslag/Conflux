// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Xunit;

namespace Conflux.API.Tests.Controllers;

public class ProductsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly HttpClient _client;

    static ProductsControllerTests()
    {
        // allow enum string values to bind correctly
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public ProductsControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProductById_ReturnsSuccess_ForValidId()
    {
        // Arrange
        ProductRequestDTO dto = new()
        {
            Schema = ProductSchema.Doi,
            Url = "https://doi.org/testproduct",
            Title = "Test Product",
            Type = ProductType.Dataset,
            Categories = [ProductCategoryType.Output],
        };

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/products", dto, JsonOptions);
        createResponse.EnsureSuccessStatusCode();

        ProductResponseDTO? createdProduct =
            await createResponse.Content.ReadFromJsonAsync<ProductResponseDTO>(JsonOptions);
        Assert.NotNull(createdProduct);

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/products/{createdProduct.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        ProductResponseDTO? product = await response.Content.ReadFromJsonAsync<ProductResponseDTO>(JsonOptions);
        Assert.NotNull(product);
        Assert.Equal(createdProduct.Id, product.Id);
        Assert.Equal(dto.Title, product.Title);
        Assert.Equal(dto.Schema, product.Schema);
        Assert.Equal(dto.Url, product.Url);
        Assert.Equal(dto.Type, product.Type);
        Assert.Equal(dto.Categories.Count, product.Categories.Count);
        Assert.Contains(ProductCategoryType.Output, product.Categories);
    }

    [Fact]
    public async Task GetProductById_ReturnsNotFound_ForInvalidId()
    {
        // Arrange
        Guid invalidId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/products/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_ReturnsCreatedProduct()
    {
        // Arrange
        ProductRequestDTO dto = new()
        {
            Schema = ProductSchema.Doi,
            Url = "https://doi.org/newproduct",
            Title = "New Test Product",
            Type = ProductType.Dataset,
            Categories = [ProductCategoryType.Output, ProductCategoryType.Internal],
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/products", dto, JsonOptions);

        // Assert
        response.EnsureSuccessStatusCode();
        ProductResponseDTO? product = await response.Content.ReadFromJsonAsync<ProductResponseDTO>(JsonOptions);
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
        ProductRequestDTO dto = new()
        {
            Schema = ProductSchema.Doi,
            Url = "https://doi.org/testupdateproduct",
            Title = "Original Test Product",
            Type = ProductType.Dataset,
            Categories = [ProductCategoryType.Output],
        };

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/products", dto, JsonOptions);
        createResponse.EnsureSuccessStatusCode();

        ProductResponseDTO? createdProduct =
            await createResponse.Content.ReadFromJsonAsync<ProductResponseDTO>(JsonOptions);
        Assert.NotNull(createdProduct);

        // Create updated product data
        ProductRequestDTO updatedProductDto = new()
        {
            Schema = ProductSchema.Doi,
            Url = "https://doi.org/updated-product",
            Title = "Updated Test Product",
            Type = ProductType.Service,
            Categories = [ProductCategoryType.Input, ProductCategoryType.Internal],
        };

        // Act
        HttpResponseMessage updateResponse = await _client.PutAsJsonAsync(
            $"/products/{createdProduct.Id}", updatedProductDto, JsonOptions);

        // Assert
        updateResponse.EnsureSuccessStatusCode();
        ProductResponseDTO? updatedProduct =
            await updateResponse.Content.ReadFromJsonAsync<ProductResponseDTO>(JsonOptions);
        Assert.NotNull(updatedProduct);
        Assert.Equal(createdProduct.Id, updatedProduct.Id);
        Assert.Equal(updatedProductDto.Title, updatedProduct.Title);
        Assert.Equal(updatedProductDto.Schema, updatedProduct.Schema);
        Assert.Equal(updatedProductDto.Url, updatedProduct.Url);
        Assert.Equal(updatedProductDto.Type, updatedProduct.Type);
        Assert.Equal(updatedProductDto.Categories.Count, updatedProduct.Categories.Count);
        Assert.Contains(ProductCategoryType.Input, updatedProduct.Categories);
        Assert.Contains(ProductCategoryType.Internal, updatedProduct.Categories);
    }

    [Fact]
    public async Task UpdateProduct_ReturnsNotFound_ForInvalidId()
    {
        // Arrange
        Guid invalidId = Guid.NewGuid();
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

        // Act
        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"/products/{invalidId}", updateDto, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_DeletesProduct()
    {
        // Arrange
        ProductRequestDTO dto = new()
        {
            Schema = ProductSchema.Doi,
            Url = "https://doi.org/testdeleteproduct",
            Title = "Test Delete Product",
            Type = ProductType.Dataset,
            Categories = [ProductCategoryType.Output],
        };

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/products", dto, JsonOptions);
        createResponse.EnsureSuccessStatusCode();

        ProductResponseDTO? createdProduct =
            await createResponse.Content.ReadFromJsonAsync<ProductResponseDTO>(JsonOptions);
        Assert.NotNull(createdProduct);

        // Act
        HttpResponseMessage deleteResponse = await _client.DeleteAsync($"/products/{createdProduct.Id}");

        // Assert
        deleteResponse.EnsureSuccessStatusCode();

        // Verify the product is deleted by trying to get it
        HttpResponseMessage getResponse = await _client.GetAsync($"/products/{createdProduct.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_ReturnsNotFound_ForInvalidId()
    {
        // Arrange
        Guid invalidId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await _client.DeleteAsync($"/products/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
