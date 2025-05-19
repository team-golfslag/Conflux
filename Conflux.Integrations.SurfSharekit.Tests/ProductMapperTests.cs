// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Net;
using Conflux;
using Conflux.Domain;
using Conflux.Integrations.SURFSharekit;
using Conflux.Integrations.SURFSharekit.Tests;
using Conflux.Integrations.SURFSharekit.Tests.Helpers;

using SURFSharekit.Net;
using SURFSharekit.Net.Models;
using SURFSharekit.Net.Models.RepoItem;
using Xunit;
using Xunit.Abstractions;

namespace Conflux.Integrations.SurfSharekit.Tests;

public class ProductMapperTests
{
    
    private readonly ITestOutputHelper _output;

    public ProductMapperTests(ITestOutputHelper output)
    {
        _output = output;
    }
    
    /// <summary>
    /// This is a test to see if the ProductMaper.MultipleRepoItemsToProducts given a list of <see cref="SURFSharekitRepoItem"/>s
    /// returns a valid list of <see cref="Product"/>s
    /// </summary>
    [Fact]
    public async Task SurfShareKit_GetMultipleToProductList_Test()
    {
        FakeHttpMessageHandler handler = new($"{{\"data\":[{SURFSharekitJsonReponse.DummyResponse}]}}", HttpStatusCode.OK);
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new("https://dummy/"),
        };
        SURFSharekitApiClient client = new(httpClient);

        var result = await client.GetAllRepoItems();

        var allProducts = ProductMapper.MultipleRepoItemsToProducts(result);
        
        Assert.Single(allProducts);
        
        Product product = allProducts[0];
        
        Assert.NotNull(product);
        Assert.NotNull(product.Title);
        Assert.Equal("Test 1 voor PoC Projectenmodule", product.Title);
        
        Assert.NotNull(product.Url);
        Assert.Equal("https://doi.org/10.1000/182", product.Url);
        
        Assert.IsType<Guid>(product.Id);
        Assert.Equal(product.Schema, ProductSchema.Doi);
    }
    
    
    /// <summary>
    /// When we receive a singular <see cref="SURFSharekitRepoItem"/> by getting it per id,
    /// ProductMapper.SingleRepoItemToProduct should return a valid <see cref="Product"/>
    /// </summary>
    [Fact]
    public async Task SurfShareKit_GetPerIdToProduct_Test()
    {
        FakeHttpMessageHandler handler = new(SURFSharekitJsonReponse.DummyResponse, HttpStatusCode.OK);
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new("https://dummy/"),
        };
        SURFSharekitApiClient client = new(httpClient);
        SURFSharekitRepoItem item = await client.GetRepoItemById("dummy-id");
        Product? product = ProductMapper.SingleRepoItemToProduct(item);
        
        Assert.NotNull(product);
        Assert.NotNull(product.Title);
        Assert.Equal("Test 1 voor PoC Projectenmodule", product.Title);
        
        Assert.NotNull(product.Url);
        Assert.Equal("https://doi.org/10.1000/182", product.Url);
        
        Assert.IsType<Guid>(product.Id);
        Assert.Equal(product.Schema, ProductSchema.Doi);
        
    }
}
