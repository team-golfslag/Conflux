// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.API.Attributes;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Microsoft.AspNetCore.Mvc;
using Conflux.Domain;
using Crossref.Net.Services;
using Microsoft.AspNetCore.Authorization;

namespace Conflux.API.Controllers;

/// <summary>
/// Controller responsible for handling product-related operations.
/// </summary>
[ApiController]
[Authorize]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
[Route("productinfo")]
public class ProductInfoController : ControllerBase
{
    private readonly IProductsService _productService;

    public ProductInfoController(IProductsService productService)
    {
        _productService = productService;
    }
    
    /// <summary>
    /// Retrieves product information based on the provided DOI (Digital Object Identifier).
    /// </summary>
    /// <param name="doi">The DOI of the product to retrieve information for.</param>
    /// <returns>
    /// An <see cref="ActionResult{ProductResponseDTO}" /> containing the product information if found, or a 404 status if not found.
    /// </returns>
    [HttpGet]
    [Route("doi")]
    [ProducesResponseType(typeof(ProductResponseDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductResponseDTO>> GetInfoFromDoi([FromQuery] string doi) =>
        await _productService.GetInfoFromDoi(doi);
    
    /// <summary>
    /// Retrieves an archive link for a product based on the provided URL.
    /// </summary>
    /// <param name="url">The URL of the product to retrieve the archive link for.</param>
    /// <returns>
    /// An <see cref="ActionResult{ProductResponseDTO}" /> containing the product information with the archive link if found, or a 404 status if not found.
    /// </returns>
    [HttpGet]
    [Route("archive")]
    [ProducesResponseType(typeof(ProductResponseDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductResponseDTO>> GetArchiveLinkForUrl([FromQuery] string url) =>
        await _productService.GetArchiveLinkForUrl(url);
}
