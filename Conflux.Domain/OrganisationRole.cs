// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Domain;

/// <summary>
/// TODO: Move to RAiD Package.
/// </summary>
public enum OrganisationRoleType
{
    LeadResearchOrganization = 182,
    OtherResearchOrganization = 183,
    PartnerOrganization = 184,
    Contractor = 185,
    Funder = 186,
    Facility = 187,
    OtherOrganization = 188,
}

[PrimaryKey(nameof(OrganisationId), nameof(Role))]
public class OrganisationRole
{
    [ForeignKey(nameof(Project))] public Guid OrganisationId { get; init; }

    [Key] public OrganisationRoleType Role { get; init; }

    public string SchemaUri => "https://vocabulary.raid.org/organisation.role.schema/359";
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string GetUri => "https://vocabulary.raid.org/organisation.role.schema/" + (int)Role;
}
