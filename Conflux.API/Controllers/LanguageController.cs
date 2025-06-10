// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Services;
using Conflux.Integrations.RAiD;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conflux.API.Controllers;

/// <summary>
/// Represents the controller for admin functionalities.
/// </summary>
[ApiController]
[Authorize]
[Route("languages")]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public class LanguageController(ILanguageService languageService) : ControllerBase
{
    /// <summary>
    /// Gets all available language codes.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public Task<ActionResult<List<string>>> GetAvailableLanguageCodes()
    {
        IEnumerable<string> languageCodes = languageService.GetAllLanguages();
        return Task.FromResult<ActionResult<List<string>>>(Ok(languageCodes.ToList()));
    }
}

