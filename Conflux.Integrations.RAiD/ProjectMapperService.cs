// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain;
using Microsoft.EntityFrameworkCore;
using RAiD.Net.Domain;

namespace Conflux.Integrations.RAiD;

public class ProjectMapperService : IProjectMapperService
{
    private readonly ConfluxContext _context;

    public ProjectMapperService(ConfluxContext context)
    {
        _context = context;
    }

    public async Task<RAiDCreateRequest> MapProjectCreationRequest(Guid projectId)
    {
        Project project = await GetProject(projectId);

        return new()
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
            AlternateIdentifier =
            [
                new()
                {
                    Id = project.Id.ToString(),
                    Type = "conflux-id",
                },
            ],
            SpatialCoverage = null, // Not implemented for now
        };
    }

    public async Task<List<RAiDIncompatibility>> CheckProjectCompatibility(Guid projectId)
    {
        Project project = await GetProject(projectId);

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
            incompatibilities.Add(new()
            {
                Type = RAiDIncompatibilityType.NoActivePrimaryTitle,
            });

        if (activeTitles.Count > 1)
            incompatibilities.Add(new()
            {
                Type = RAiDIncompatibilityType.MultipleActivePrimaryTitle,
            });

        // Constraint: Titles have maximum 100 characters
        // Source: https://metadata.raid.org/en/latest/core/titles.html#title-text
        incompatibilities.AddRange(project.Titles
            .Where(t => t.Text.Length > 100)
            .Select(t => new RAiDIncompatibility
            {
                Type = RAiDIncompatibilityType.ProjectTitleTooLong,
                ObjectId = t.Id,
            }).ToList());

        // Constraint: Descriptions have maximum 1000 characters
        // Source: https://metadata.raid.org/en/latest/core/descriptions.html#description-text
        incompatibilities.AddRange(project.Descriptions
            .Where(t => t.Text.Length > 1000)
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
                incompatibilities.Add(new()
                {
                    Type = RAiDIncompatibilityType.NoPrimaryDescription,
                });
            if (primaryDescriptions.Count > 1)
                incompatibilities.Add(new()
                {
                    Type = RAiDIncompatibilityType.MultiplePrimaryDescriptions,
                });
        }

        // Requirement: at least one contributor is mandatory
        // Source: https://metadata.raid.org/en/latest/core/contributors.html#contributor
        if (project.Contributors.Count == 0)
            incompatibilities.Add(new()
            {
                Type = RAiDIncompatibilityType.NoContributors,
            });

        // In Conflux people are not required to have an ORCiD
        incompatibilities.AddRange(project.Contributors
            .Where(c => string.IsNullOrEmpty(_context.People.Find(c.PersonId)!.ORCiD))
            .Select(c => new RAiDIncompatibility
            {
                Type = RAiDIncompatibilityType.ContributorWithoutOrcid,
                ObjectId = c.PersonId,
            }));

        // Constraints: contributors must have one and only one position at any given time (contributors may also be flagged as a 'leader' or 'contact' separately)
        // Source: https://metadata.raid.org/en/latest/core/contributors.html#contributor-position
        incompatibilities.AddRange(project.Contributors
            .Where(c =>
            {
                if (c.Positions.Count == 0)
                    return false;
                c.Positions.Sort((p, q) => DateTime.Compare(p.StartDate, q.StartDate));
                DateTime? last = c.Positions[0].StartDate;
                foreach (ContributorPosition position in c.Positions)
                {
                    // Previous had no end and was not the last
                    if (last == null)
                        return true;
                    // Previous ended after current started
                    if (last > position.StartDate)
                        return true;
                    // Previous ended 
                    if (last > position.EndDate)
                        return true;
                    last = position.EndDate;
                }

                return false;
            })
            .Select(c => new RAiDIncompatibility
            {
                Type = RAiDIncompatibilityType.OverlappingContributorPositions,
                ObjectId = c.PersonId,
            }));

        // Requirement: at least one contributor must be flagged as a project leader
        // Source: https://metadata.raid.org/en/latest/core/contributors.html#contributor-leader
        if (!project.Contributors.Any(c => c.Leader))
            incompatibilities.Add(new()
            {
                Type = RAiDIncompatibilityType.NoProjectLeader,
            });

        // Requirement: at least one contributor must be flagged as a project contact
        // Source: https://metadata.raid.org/en/latest/core/contributors.html#contributor-contact
        if (!project.Contributors.Any(c => c.Contact))
            incompatibilities.Add(new()
            {
                Type = RAiDIncompatibilityType.NoProjectContact,
            });

        // RAiD requires a ROR to be set for all Organisations
        incompatibilities.AddRange(project.Organisations.Where(p => string.IsNullOrEmpty(p.Organisation!.RORId))
            .Select(o => new RAiDIncompatibility
            {
                Type = RAiDIncompatibilityType.OrganisationWithoutRor,
                ObjectId = o.OrganisationId,
            }));
        
        // Note: An organisation's role may change over time, but each organisation may have one and only one role at any given time.
        // Source: https://metadata.raid.org/en/latest/core/organisations.html#organisation-role
        incompatibilities.AddRange(project.Organisations
            .Where(c =>
            {
                if (c.Roles.Count == 0)
                    return false;
                c.Roles.Sort((p, q) => DateTime.Compare(p.StartDate, q.StartDate));
                DateTime? last = c.Roles[0].StartDate;
                foreach (OrganisationRole role in c.Roles)
                {
                    // Previous had no end and was not the last
                    if (last == null)
                        return true;
                    // Previous ended after current started
                    if (last > role.StartDate)
                        return true;
                    // Previous ended 
                    if (last > role.EndDate)
                        return true;
                    last = role.EndDate;
                }

                return false;
            })
            .Select(o => new RAiDIncompatibility
            {
                Type = RAiDIncompatibilityType.OverlappingOrganisationRoles,
                ObjectId = o.OrganisationId,
            }));

        // Constraints: one (and only one) Organisation must be designated as 'Lead Research Organisation'
        // Source: https://metadata.raid.org/en/latest/core/organisations.html#organisation-role-id
        List<OrganisationRole> leadOrganisationsRoles = project.Organisations.SelectMany(o =>
                o.Roles.Where(r => r.Role == OrganisationRoleType.LeadResearchOrganization))
            .ToList();
        
        if (!leadOrganisationsRoles.Any(r => r.StartDate <= project.StartDate
            && (r.EndDate == null || r.EndDate >= project.EndDate)))
            incompatibilities.Add(new()
            {
                Type = RAiDIncompatibilityType.NoLeadResearchOrganisation,
            });
        

        incompatibilities.AddRange(project.Products
            .Where(p => p.Categories.Count == 0)
            .Select(p => new RAiDIncompatibility
            {
                Type = RAiDIncompatibilityType.NoProductCategory,
                ObjectId = p.Id,
            }));

        return incompatibilities;
    }

    public async Task<RAiDUpdateRequest> MapProjectUpdateRequest(Guid projectId)
    {
        Project project = await GetProject(projectId);

        return new()
        {
            Metadata = null,
            Identifier = MapRAiDInfo(project.RAiDInfo!),
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
            AlternateUrl = null,
            Contributor = project.Contributors.ConvertAll(MapContributor),
            Organisation = project.Organisations.ConvertAll(MapOrganisation),
            Subject = null,     // Not implemented for now
            RelatedRaid = null, // Not implemented for now
            RelatedObject = project.Products.ConvertAll(MapProduct),
            AlternateIdentifier =
            [
                new()
                {
                    Id = project.Id.ToString(),
                    Type = "conflux-id",
                },
            ],
            SpatialCoverage = null, // Not implemented for now
        };
    }

    private async Task<Project> GetProject(Guid projectId)
    {
        Project? project = await _context.Projects.Where(p => p.Id == projectId)
            .Include(p => p.Contributors)
            .ThenInclude(c => c.Positions)
            .Include(p => p.Contributors)
            .ThenInclude(c => c.Roles)
            .Include(p => p.Descriptions)
            .Include(p => p.Titles)
            .Include(p => p.Organisations)
            .ThenInclude(o => o.Organisation)
            .Include(p => p.Organisations)
            .ThenInclude(o => o.Roles)
            .Include(p => p.Products)
            .Include(p => p.RAiDInfo)
            .FirstOrDefaultAsync();

        if (project == null)
            throw new ArgumentException($"Project with Id {projectId} could not be found.");

        return project;
    }

    private static RAiDId MapRAiDInfo(RAiDInfo raidInfo) =>
        new()
        {
            IdValue = raidInfo.RAiDId,
            SchemaUri = raidInfo.SchemaUri,
            RegistrationAgency = new()
            {
                Id = raidInfo.RegistrationAgencyId,
                SchemaUri = raidInfo.RegistrationAgencySchemaUri,
            },
            Owner = new()
            {
                Id = raidInfo.OwnerId,
                SchemaUri = raidInfo.OwnerSchemaUri,
                ServicePoint = raidInfo.OwnerServicePoint,
            },
            RaidAgencyUrl = raidInfo.RegistrationAgencyId,
            License = raidInfo.License,
            Version = raidInfo.Version,
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
            Id = person.ORCiD,
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

    private RAiDOrganisation MapOrganisation(ProjectOrganisation projectOrganisation)
    {
        return new()
        {
            Id = projectOrganisation.Organisation!.RORId ?? throw new ArgumentNullException(nameof(projectOrganisation.OrganisationId)),
            SchemaUri = projectOrganisation.Organisation!.SchemaUri,
            Role = projectOrganisation.Roles.Select(MapOrganisationRole).ToList(),
        };
    }

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
                SchemaUri = Product.TypeSchemaUri,
            },
            Category = product.Categories.ToList().ConvertAll(p => new RAiDRelatedObjectCategory
            {
                Id = Product.GetCategoryUri(p),
                SchemaUri = Product.CategorySchemaUri,
            }),
        };
}
