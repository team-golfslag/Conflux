// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Microsoft.AspNetCore.Mvc;

namespace Conflux.Domain.Logic.DTOs.Queries;

/// <summary>
/// The Data Transfer Object for querying a <see cref="Project" />.
/// </summary>
#pragma warning disable S101 // Types should be named in camel case
public class ProjectQueryDTO
#pragma warning restore S101
{
    [FromQuery] public string? Query { get; init; }

    [FromQuery(Name = "start_date")] public DateTime? StartDate { get; init; }

    [FromQuery(Name = "end_date")] public DateTime? EndDate { get; init; }

    [FromQuery(Name = "order_by")] public OrderByType? OrderByType { get; init; }
}

public enum OrderByType
{
    TitleAsc,
    TitleDesc,
    StartDateAsc,
    StartDateDesc,
    EndDateAsc,
    EndDateDesc,
}
