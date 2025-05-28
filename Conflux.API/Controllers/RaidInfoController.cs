// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Services;
using Conflux.Integrations.RAiD;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conflux.API.Controllers;

[Route("projects/{projectId:guid}/raid")]
[ApiController]
[Authorize]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public class RaidInfoController : ControllerBase
{
    private readonly IRaidInfoService _service;

    public RaidInfoController(IRaidInfoService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<RAiDInfoResponseDTO> GetRaidInfoByProjectId([FromRoute] Guid projectId) =>
        await _service.GetRAiDInfoByProjectId(projectId);

    [HttpGet]
    [Route("incompatibilities")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<RAiDIncompatibility>> GetRaidIncompatibilities([FromRoute] Guid projectId) =>
        await _service.GetRAiDIncompatibilities(projectId);

    [HttpPost]
    [Route("mint")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<RAiDInfoResponseDTO> MintRaid([FromRoute] Guid projectId) =>
        await _service.MintRAiDAsync(projectId);

    [HttpPost]
    [Route("update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<RAiDInfoResponseDTO> SyncRaid([FromRoute] Guid projectId) =>
        await _service.SyncRAiDAsync(projectId);
}
