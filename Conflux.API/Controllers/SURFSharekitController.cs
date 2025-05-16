// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Web.Http.Controllers;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.DTOs.Patch;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Session;
using Microsoft.AspNet.WebHooks;
using Microsoft.AspNet.WebHooks.Config;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SURFSharekit.Net.Models;
using SURFSharekit.Net.Models.RepoItem;

namespace Conflux.API.Controllers;

[Route("sharekit/")]
public class SURFSharekitController : ControllerBase
{
    private readonly SURFSharekitService _sharekitService;

    public SURFSharekitController(SURFSharekitService sharekitService)
    {
        _sharekitService = sharekitService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(HttpResponse), StatusCodes.Status200OK)]
    [Route("webhook/")]
    public async Task<ActionResult<string>> HandleWebhook(
        [FromBody] SURFSharekitRepoItem payload)
    {
        return _sharekitService.HandleWebhook(payload);
    }

    [HttpGet]
    [ProducesResponseType(typeof(HttpResponse), StatusCodes.Status200OK)]
    [Route("update/")]
    public async Task<ActionResult<List<string>>> UpdateRepoItems()
    {
        return await _sharekitService.UpdateRepoItems();
    }
}