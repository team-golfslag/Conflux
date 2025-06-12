// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Microsoft.AspNetCore.Mvc;

namespace Conflux.Domain.Logic.DTOs.Queries;

/// <summary>
/// Query + export‐options for CSV. All include_XXX flags default to false,
/// except Id which is always exported.
/// </summary>
public class ProjectCsvRequestDTO : ProjectQueryDTO
{
    [FromQuery(Name = "include_start_date")]
    public bool IncludeStartDate { get; init; } = true;
    
    [FromQuery(Name = "include_end_date")] public bool IncludeEndDate { get; init; } = true;
    
    [FromQuery(Name = "include_users")] public bool IncludeUsers { get; init; } = true;
    
    [FromQuery(Name = "include_contributors")]
    public bool IncludeContributors { get; init; } = true;
    
    [FromQuery(Name = "include_products")] public bool IncludeProducts { get; init; } = true;
    
    [FromQuery(Name = "include_organisations")]
    public bool IncludeOrganisations { get; init; } = true;
    
    [FromQuery(Name = "include_title")] public bool IncludeTitle { get; init; } = true;
    
    [FromQuery(Name = "include_description")]
    public bool IncludeDescription { get; init; } = true;
    
    [FromQuery(Name = "include_lectorate")]
    public bool IncludeLectorate { get; init; } = true;
    
    [FromQuery(Name = "include_owner_organisation")]
    public bool IncludeOwnerOrganisation { get; init; } = true;
}
