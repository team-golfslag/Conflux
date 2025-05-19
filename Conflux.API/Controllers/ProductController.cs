// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Services;
using Microsoft.AspNetCore.Mvc;

namespace Conflux.API.Controllers;

[ApiController]
[Route("products")]
public class ProductController : ControllerBase
{
    private readonly IProductsService _productService;

    public ProductController(IProductsService productService)
    {
        _productService = productService;
    }
    
    [HttpGet("query")]
    [Route("api/products")]
    public async Task<IActionResult> GetProductsByQueryAsync(string? query)
    {
        List<ProductResponseDTO> products = await _productService.GetProductsByQueryAsync(query);
        return Ok(products);
    }
    
    [HttpGet]
    [Route("{productId:guid}")]
    public async Task<IActionResult> GetProductByIdAsync(Guid productId)
    {
        ProductResponseDTO product = await _productService.GetProductByIdAsync(productId);
        return Ok(product);
    }
}
