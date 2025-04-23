// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain.Logic.DTOs;

/// <summary>
/// The Data Transfer Object for <see cref="Contributor" />
/// </summary>
#pragma warning disable S101 // Types should be named in camel case
public class ContributorPostDTO
#pragma warning restore S101
{
    [Required] public required string Name { get; init; }

    /// <summary>
    /// Converts a <see cref="ContributorPostDTO" /> to a <see cref="Contributor" />
    /// </summary>
    /// <returns>The converted <see cref="Contributor" /></returns>
    public Contributor ToContributor() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = Name,
        };
}
