// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.API.Attributes;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conflux.API.Controllers;

/// <summary>
/// Represents the controller for managing contributors
/// </summary>
[ApiController]
[Authorize]
[Route("admin")]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>
    /// Gets all users whose name or email contains the query (case-insensitive).
    /// </summary>
    /// <param name="query">Optional: The string to search in the name or email</param>
    /// <param name="adminsOnly">If true, only returns users with SystemAdmin tier or higher</param>
    /// <returns>Filtered list of users</returns>
    [HttpGet]
    [Route("users")]
    [ProducesResponseType(typeof(List<UserResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserResponseDTO>>> GetUsersByQuery([FromQuery] string? query, bool adminsOnly) =>
        await _adminService.GetUsersByQuery(query, adminsOnly);

    /// <summary>
    /// Makes a user a system administrator.
    /// </summary>
    /// <param name="userId">The GUID of the user</param>
    /// <param name="tier">The tier to set for the user</param>
    /// <returns>The updated user response</returns>
    [HttpPost]
    [Route("make-admin")]
    [ProducesResponseType(typeof(UserResponseDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserResponseDTO>> SetUserTier([FromQuery] Guid userId, [FromBody] UserTier tier) =>
        await _adminService.SetUserTier(userId, tier);

    /// <summary>
    /// Gets the list of lectorates that are available for selection in the UI.
    /// </summary>
    [HttpGet]
    [Route("lectorates")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetLectorates() => 
        await _adminService.GetAvailableLectorates();
    
    /// <summary>
    /// Gets the list of organisations that are available for selection in the UI.
    /// </summary>
    [HttpGet]
    [Route("organisations")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetOrganisations() => 
        await _adminService.GetAvailableOrganisations();
    
    /// <summary>
    /// Assigns lectorates to a user.
    /// </summary>
    /// <param name="userId">The GUID of the user</param>
    /// <param name="lectorates">The list of lectorates to assign</param>
    /// <returns>The updated user response</returns>
    [HttpPost]
    [Route("assign-lectorates")]
    [ProducesResponseType(typeof(UserResponseDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserResponseDTO>> AssignLectoratesToUser([FromQuery] Guid userId, [FromBody] List<string> lectorates) => 
        await _adminService.AssignLectoratesToUser(userId, lectorates);

    /// <summary>
    /// Assigns organisations to a user.
    /// </summary>
    /// <param name="userId">The GUID of the user</param>
    /// <param name="organisations">The list of organisations to assign</param>
    /// <returns>The updated user response</returns>
    [HttpPost]
    [Route("assign-organisations")]
    [ProducesResponseType(typeof(UserResponseDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserResponseDTO>> AssignOrganisationsToUser([FromQuery] Guid userId, [FromBody] List<string> organisations) => 
        await _adminService.AssignOrganisationsToUser(userId, organisations);
}
