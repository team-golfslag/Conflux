// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Integrations.RAiD;

public enum RAiDIncompatibilityType
{
    NoActivePrimaryTitle,
    MultipleActivePrimaryTitle,
    ProjectTitleTooLong,
    NoPrimaryDescription,
    MultiplePrimaryDescriptions,
    ProjectDescriptionTooLong,
    NoContributors,
    ContributorWithoutOrcid,
    OverlappingContributorPositions,
    NoProjectLeader,
    NoProjectContact,
    OverlappingOrganisationRoles,
    NoLeadResearchOrganisation,
    MultipleLeadResearchOrganisation,
    NoProductCategory,
}

public class RAiDIncompatibility
{
    public RAiDIncompatibilityType Type { get; init; }
    public Guid ObjectId { get; init; }
}
