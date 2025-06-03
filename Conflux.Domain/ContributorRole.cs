// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Domain;

/// <summary>
/// Represents the Contributor Role Taxonomy (CRedIT) roles.
/// See https://credit.niso.org/ for more information.
/// TODO: Move to RAiD Package.
/// </summary>
public enum ContributorRoleType
{
    Conceptualization,
    DataCuration,
    FormalAnalysis,
    FundingAcquisition,
    Investigation,
    Methodology,
    ProjectAdministration,
    Resources,
    Software,
    Supervision,
    Validation,
    Visualization,
    WritingOriginalDraft,
    WritingReviewEditing,
}

[PrimaryKey(nameof(PersonId), nameof(ProjectId), nameof(RoleType))]
public class ContributorRole
{
    public Guid PersonId { get; init; }
    
    public Guid ProjectId { get; init; }
    
    public Contributor? Contributor { get; init; }

    [Key] public required ContributorRoleType RoleType { get; init; }

    public string SchemaUri => "https://credit.niso.org/";

    /// <summary>
    /// Get the URI for the Contributor Role.
    /// contributor-roles/ MUST be contributor-role/
    /// </summary>
    /// <returns>The URI for the Contributor Role.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the RoleType is not a valid ContributorRoleType.</exception>
    public string GetUri()
    {
        return SchemaUri + "contributor-roles/" + RoleType switch
        {
            ContributorRoleType.Conceptualization     => "conceptualization/",
            ContributorRoleType.DataCuration          => "data-curation/",
            ContributorRoleType.FormalAnalysis        => "formal-analysis/",
            ContributorRoleType.FundingAcquisition    => "funding-acquisition/",
            ContributorRoleType.Investigation         => "investigation/",
            ContributorRoleType.Methodology           => "methodology/",
            ContributorRoleType.ProjectAdministration => "project-administration/",
            ContributorRoleType.Resources             => "resources/",
            ContributorRoleType.Software              => "software/",
            ContributorRoleType.Supervision           => "supervision/",
            ContributorRoleType.Validation            => "validation/",
            ContributorRoleType.Visualization         => "visualization/",
            ContributorRoleType.WritingOriginalDraft  => "writing-original-draft/",
            ContributorRoleType.WritingReviewEditing  => "writing-review-editing/",
            _                                         => throw new ArgumentOutOfRangeException(),
        };
    }
}
