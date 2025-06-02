// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.API.Attributes;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Conflux.API.Tests.Controllers;

[Route("projects/{projectId:guid}/descriptions")]
[ApiController]
[Authorize]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
[RouteParamName("projectId")]
public class ProjectDescriptionsController : ControllerBase
{
    private readonly IProjectDescriptionsService _descriptionsService;

    public ProjectDescriptionsController(IProjectDescriptionsService descriptionsService)
    {
        _descriptionsService = descriptionsService;
    }

    /// <summary>
    /// Gets all descriptions for a project
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <returns>List of project descriptions</returns>
    [HttpGet]
    [RequireProjectRole(UserRoleType.User)]
    [ProducesResponseType(typeof(List<ProjectDescriptionResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProjectDescriptionResponseDTO>>> GetDescriptions(Guid projectId) =>
        await _descriptionsService.GetDescriptionsByProjectIdAsync(projectId);

    /// <summary>
    /// Gets a description by its GUID
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="descriptionId">The GUID of the description</param>
    /// <returns>The description</returns>
    [HttpGet("{descriptionId:guid}")]
    [RequireProjectRole(UserRoleType.User)]
    [ProducesResponseType(typeof(ProjectDescriptionResponseDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectDescriptionResponseDTO>>
        GetDescriptionById(Guid projectId, Guid descriptionId) =>
        await _descriptionsService.GetDescriptionByIdAsync(projectId, descriptionId);

    /// <summary>
    /// Creates a new description for a project
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="descriptionDTO">The description data</param>
    /// <returns>The created description</returns>
    [HttpPost]
    [RequireProjectRole(UserRoleType.Admin)]
    [ProducesResponseType(typeof(ProjectDescriptionResponseDTO), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProjectDescriptionResponseDTO>> CreateDescription(Guid projectId,
        ProjectDescriptionRequestDTO descriptionDTO)
    {
        ProjectDescriptionResponseDTO createdDescription =
            await _descriptionsService.CreateDescriptionAsync(projectId, descriptionDTO);

        return CreatedAtAction(
            nameof(GetDescriptionById),
            new
            {
                projectId,
                descriptionId = createdDescription.Id,
            },
            createdDescription);
    }

    /// <summary>
    /// Updates a description
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="descriptionId">The GUID of the description</param>
    /// <param name="descriptionDTO">The updated description data</param>
    /// <returns>The updated description</returns>
    [HttpPut("{descriptionId:guid}")]
    [RequireProjectRole(UserRoleType.Admin)]
    [ProducesResponseType(typeof(ProjectDescriptionResponseDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectDescriptionResponseDTO>> UpdateDescription(Guid projectId, Guid descriptionId,
        ProjectDescriptionRequestDTO descriptionDTO) =>
        await _descriptionsService.UpdateDescriptionAsync(projectId, descriptionId, descriptionDTO);

    /// <summary>
    /// Deletes a description
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="descriptionId">The GUID of the description</param>
    /// <returns>No content result</returns>
    [HttpDelete("{descriptionId:guid}")]
    [RequireProjectRole(UserRoleType.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDescription(Guid projectId, Guid descriptionId)
    {
        await _descriptionsService.DeleteDescriptionAsync(projectId, descriptionId);
        return NoContent();
    }
}
