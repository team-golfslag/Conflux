// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conflux.API.Controllers;

/// <summary>
/// Controller for managing project organisations
/// </summary>
[ApiController]
[Route("projects/{projectId:guid}/organisations")]
public class ProjectOrganisationsController : ControllerBase
{
    private readonly IProjectOrganisationsService _projectOrganisationsService;

    public ProjectOrganisationsController(IProjectOrganisationsService projectOrganisationsService)
    {
        _projectOrganisationsService = projectOrganisationsService;
    }

    /// <summary>
    /// Gets all organisations for a project
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <returns>A list of organisations for the project</returns>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(List<OrganisationResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ProjectOrganisationResponseDTO>>> GetOrganisations(Guid projectId) =>
        await _projectOrganisationsService.GetOrganisationsByProjectIdAsync(projectId);

    /// <summary>
    /// Gets a specific organisation for a project
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="organisationId">The GUID of the organisation</param>
    /// <returns>The organisation details</returns>
    [HttpGet("{organisationId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(OrganisationResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectOrganisationResponseDTO>>
        GetOrganisationById(Guid projectId, Guid organisationId) =>
        await _projectOrganisationsService.GetOrganisationByIdAsync(projectId, organisationId);

    /// <summary>
    /// Creates a new organisation for a project
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="organisationDto">The organisation data</param>
    /// <returns>The created organisation</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(OrganisationResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrganisationResponseDTO>> CreateOrganisation(
        Guid projectId,
        OrganisationRequestDTO organisationDto)
    {
        ProjectOrganisationResponseDTO createdOrganisation =
            await _projectOrganisationsService.CreateOrganisationAsync(projectId, organisationDto);

        return CreatedAtAction(
            nameof(GetOrganisationById),
            new
            {
                projectId,
                organisationId = createdOrganisation.Organisation.Id,
            },
            createdOrganisation);
    }

    /// <summary>
    /// Updates an existing organisation for a project
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="organisationId">The GUID of the organisation</param>
    /// <param name="organisationDto">The updated organisation data</param>
    /// <returns>The updated organisation</returns>
    [HttpPut("{organisationId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(OrganisationResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectOrganisationResponseDTO>> UpdateOrganisation(
        Guid projectId,
        Guid organisationId,
        OrganisationRequestDTO organisationDto) =>
        await _projectOrganisationsService.UpdateOrganisationAsync(projectId, organisationId, organisationDto);

    /// <summary>
    /// Deletes an organisation from a project
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="organisationId">The GUID of the organisation</param>
    /// <returns>No content</returns>
    [HttpDelete("{organisationId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOrganisation(Guid projectId, Guid organisationId)
    {
        await _projectOrganisationsService.DeleteOrganisationAsync(projectId, organisationId);
        return NoContent();
    }
}
