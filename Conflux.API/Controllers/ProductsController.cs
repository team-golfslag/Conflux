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
[RouteParamName("projectId")]
[Route("projects/{projectId:guid}/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductsService _productService;

    public ProductsController(IProductsService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Retrieves a specific product based on the provided product ID.
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="productId">The unique identifier of the product to retrieve.</param>
    /// <returns>A <see cref="ProductResponseDTO" /> object representing the product with the specified ID.</returns>
    [HttpGet]
    [Route("{productId:guid}")]
    [RequireProjectRole(UserRoleType.User)]
    [ProducesResponseType(typeof(ProductResponseDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductResponseDTO>> GetProductByIdAsync([FromRoute] Guid projectId,
        [FromRoute] Guid productId) =>
        await _productService.GetProductByIdAsync(projectId, productId);

    /// <summary>
    /// Creates a new product based on the provided product details.
    /// </summary>
    /// <param name="projectId">The ID of the related <see cref="Project"/></param>
    /// <param name="productDTO">
    /// An instance of <see cref="ProductRequestDTO" /> containing the details of the product to be
    /// created.
    /// </param>
    /// <returns>
    /// An instance of <see cref="ProductResponseDTO" /> representing the created product with its attributes and
    /// unique identifier.
    /// </returns>
    [HttpPost]
    [RequireProjectRole(UserRoleType.Admin)]
    [ProducesResponseType(typeof(ProductResponseDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductResponseDTO>> CreateProductAsync([FromRoute] Guid projectId,
        [FromBody] ProductRequestDTO productDTO) =>
        await _productService.CreateProductAsync(projectId, productDTO);

    /// <summary>
    /// Updates the details of an existing product identified by the specified product ID.
    /// </summary>
    /// <param name="projectId">The ID of the related <see cref="Project"/></param>
    /// <param name="productId">The unique identifier of the product to be updated.</param>
    /// <param name="productDTO">The updated product details encapsulated in a <see cref="ProductRequestDTO" /> object.</param>
    /// <returns>A <see cref="ProductResponseDTO" /> object representing the updated product.</returns>
    [HttpPut]
    [Route("{productId:guid}")]
    [RequireProjectRole(UserRoleType.Admin)]
    [ProducesResponseType(typeof(ProductResponseDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductResponseDTO>> UpdateProductAsync([FromRoute] Guid projectId,
        [FromRoute] Guid productId,
        [FromBody] ProductRequestDTO productDTO) =>
        await _productService.UpdateProductAsync(projectId, productId, productDTO);

    /// <summary>
    /// Deletes a product identified by the given product ID.
    /// </summary>
    /// <param name="projectId">The ID of the related <see cref="Project"/></param>
    /// <param name="productId">The unique identifier of the product to be deleted.</param>
    /// <returns>The request response. Returns a 404 status if the product is not found or a 200 status on successful deletion.</returns>
    [HttpDelete]
    [Route("{productId:guid}")]
    [RequireProjectRole(UserRoleType.Admin)]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    public async Task<ActionResult> DeleteProductAsync([FromRoute] Guid projectId, [FromRoute] Guid productId)
    {
        try
        {
            await _productService.DeleteProductAsync(projectId, productId);
        }
        catch (ProductNotFoundException)
        {
            return NotFound("Product not found");
        }

        return Ok();
    }
}
