// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain;
using RAiD.Net.Domain;

namespace Conflux.Integrations.RAiD;

public class ProjectMapperService : IProjectMapperService
{
    private readonly ConfluxContext _context;

    public ProjectMapperService(ConfluxContext context)
    {
        _context = context;
    }

    public RAiDCreateRequest MapProjectCreationRequest(Project project) =>
        new()
        {
            Title = project.Titles.ConvertAll(MapProjectTitle),
            Date = new()
            {
                StartDate = project.StartDate,
                EndDate = project.EndDate,
            },

            Description = project.Descriptions.ConvertAll(MapProjectDescription),
            Access = new()
            {
                Type = new() // TODO make an enum for this
                {
                    Id = "https://vocabularies.coar-repositories.org/access_rights/c_abf2/",
                    SchemaUri = "https://vocabularies.coar-repositories.org/access_rights/",
                },
                EmbargoExpiry = null, // Not implemented for now
                Statement = null,     // Not implemented for now
            },
            Contributor = project.Contributors.ConvertAll(MapContributor),
            Organisation = project.Organisations.ConvertAll(MapOrganisation),
            Subject = null,     // Not implemented for now
            RelatedRaid = null, // Not implemented for now
            RelatedObject = project.Products.ConvertAll(MapProduct),
            AlternateIdentifier = null, // Not implemented for now
            SpatialCoverage = null,     // Not implemented for now
        };

    private static RAiDTitle MapProjectTitle(ProjectTitle title) =>
        new()
        {
            Text = title.Text,
            Type = new()
            {
                Id = title.TypeUri,
                SchemaUri = title.TypeSchemaUri,
            },
            StartDate = title.StartDate,
            EndDate = title.EndDate,
            Language = title.Language == null
                ? null
                : MapLanguage(title.Language),
        };

    private static RAiDDescription MapProjectDescription(ProjectDescription desc) =>
        new()
        {
            Text = desc.Text,
            Type = new()
            {
                Id = desc.TypeUri,
                SchemaUri = desc.TypeSchemaUri,
            },
            Language = desc.Language == null ? null : MapLanguage(desc.Language),
        };

    private static RAiDContributorRole MapContributorRole(ContributorRole role) =>
        new()
        {
            SchemaUri = role.SchemaUri,
            Id = role.GetUri(),
        };

    private static RAiDContributorPosition MapContributorPosition(ContributorPosition position) =>
        new()
        {
            SchemaUri = position.SchemaUri,
            Id = position.GetUri,
            StartDate = position.StartDate,
            EndDate = position.EndDate,
        };

    private RAiDContributor MapContributor(Contributor contributor)
    {
        Person person = _context.People
                .FirstOrDefault(p => p.Id == contributor.PersonId)
            ?? throw new ArgumentNullException(nameof(contributor));

        return new()
        {
            SchemaUri = person.SchemaUri,
            Email = person.Email,
            Uuid = null,
            Position = contributor.Positions.Select(MapContributorPosition).ToList(),
            Role = contributor.Roles.Select(MapContributorRole).ToList(),
            Leader = contributor.Leader,
            Contact = contributor.Contact,
        };
    }

    private static RAiDOrganisationRole MapOrganisationRole(OrganisationRole role) =>
        new()
        {
            SchemaUri = role.SchemaUri,
            Id = role.GetUri,
            StartDate = role.StartDate,
            EndDate = role.EndDate,
        };

    private static RAiDOrganisation MapOrganisation(Organisation organisation) =>
        new()
        {
            Id = organisation.RORId ?? throw new ArgumentNullException(nameof(organisation)),
            SchemaUri = organisation.SchemaUri,
            Role = organisation.Roles.Select(MapOrganisationRole).ToList(),
        };

    private static RAiDLanguage MapLanguage(Language lang) =>
        new()
        {
            Id = lang.Id,
            SchemaUri = lang.SchemaUri,
        };

    private static RAiDRelatedObject MapProduct(Product product) => 
        new()
        {
            Id = product.Url,
            SchemaUri = product.SchemaUri,
            Type = new()
            {
                Id = product.GetTypeUri,
                SchemaUri = product.TypeSchemaUri,
            },
            Category = null,
        };

    public List<RAiDIncompatibility> CheckProjectCompatibility(Project project)
    {
        List<RAiDIncompatibility> incompatibilities = [];
        
        List<ProjectTitle> activeTitles = project.Titles.Where(t => t.StartDate <= DateTime.Now
            && (t.EndDate == null || t.EndDate >= DateTime.Now)
            && t.Type == TitleType.Primary).ToList();
        
        // Note: One (and only one) current (as per start-end dates)
        // Primary Title is mandatory for each Title specified;
        // additional titles are optional; any previous titles are managed
        // by start-end dates (title type does not change).
        //
        // Source: https://metadata.raid.org/en/latest/core/titles.html#title-type-id
        if (activeTitles.Count == 0)
            incompatibilities.Add(new() {Type = RAiDIncompatibilityType.NoActivePrimaryTitle});
        
        if (activeTitles.Count > 1)
            incompatibilities.Add(new() {Type = RAiDIncompatibilityType.MultipleActivePrimaryTitle});
        
        // Constraint: Titles have maximum 100 characters
        // Source: https://metadata.raid.org/en/latest/core/titles.html#title-text
        incompatibilities.AddRange(project.Titles
            .Where(t => t.Text.Length  > 100)
            .Select(t => new RAiDIncompatibility
            {
                Type = RAiDIncompatibilityType.ProjectTitleTooLong,
                ObjectId = t.Id,
            }).ToList());
        
        // Constraint: Descriptions have maximum 1000 characters
        // Source: https://metadata.raid.org/en/latest/core/descriptions.html#description-text
        incompatibilities.AddRange(project.Descriptions
            .Where(t => t.Text.Length  > 1000)
            .Select(d => new RAiDIncompatibility
            {
                Type = RAiDIncompatibilityType.ProjectDescriptionTooLong,
                ObjectId = d.Id,
            }).ToList()); 
        
        // Constraints: if a description is provided, one (and only one) primary description is mandatory
        // Source: https://metadata.raid.org/en/latest/core/descriptions.html#description-type-id
        if (project.Descriptions.Count > 0)
        {
            List<ProjectDescription> primaryDescriptions =
                project.Descriptions.Where(d => d.Type == DescriptionType.Primary).ToList();
            if (primaryDescriptions.Count == 0)
                incompatibilities.Add(new() {Type = RAiDIncompatibilityType.NoPrimaryDescription});
            if (primaryDescriptions.Count > 1)
                incompatibilities.Add(new() {Type = RAiDIncompatibilityType.MultiplePrimaryDescriptions});
        }
        
        // Requirement: at least one contributor is mandatory
        // Source: https://metadata.raid.org/en/latest/core/contributors.html#contributor
        if (project.Contributors.Count == 0)
            incompatibilities.Add(new() {Type = RAiDIncompatibilityType.NoContributors});
        
        // Requirement: at least one contributor must be flagged as a project leader
        // Source: https://metadata.raid.org/en/latest/core/contributors.html#contributor-leader
        if (!project.Contributors.Any(c => c.Leader))
            incompatibilities.Add(new() { Type = RAiDIncompatibilityType.NoProjectLeader });
        
        // Requirement: at least one contributor must be flagged as a project contact
        // Source: https://metadata.raid.org/en/latest/core/contributors.html#contributor-contact
        if (!project.Contributors.Any(c => c.Contact))
            incompatibilities.Add(new() { Type = RAiDIncompatibilityType.NoProjectContact });
        
        // project.Organisations.Any(o => o.Roles.)
        
        
        
        

        return incompatibilities;
    }
}
