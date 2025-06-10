// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain;
using Microsoft.EntityFrameworkCore;
using Moq;
using RAiD.Net.Domain;

namespace Conflux.Integrations.RAiD.Tests;

public class ProjectMapperServiceTests
{
    private readonly ConfluxContext _context;
    private readonly ProjectMapperService _service;

    public ProjectMapperServiceTests()
    {
        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.CreateVersion7()}")
            .Options;

        Mock<ILanguageService> languageServiceMock = new();
        languageServiceMock
            .Setup(s => s.IsValidLanguageCode(It.IsAny<string>()))
            .Returns(true);
        
        _context = new(options);
        _service = new(_context, languageServiceMock.Object);
    }

    [Fact]
    public async Task MapProjectCreationRequest_MapsProjectCorrectly()
    {
        Project project = await CreateProject();

        DateTime start = project.StartDate;


        RAiDCreateRequest result = await _service.MapProjectCreationRequest(project.Id);
        Assert.NotNull(result.Date);
        Assert.Equal(start, result.Date.StartDate);
        Assert.Null(result.Date.EndDate);

        Assert.NotNull(result.Contributor);
        Assert.Single(result.Contributor);
        RAiDContributor cont = result.Contributor[0];
        Assert.Equal("https://orcid.org/0000-0002-1825-0097", cont.Id);
        Assert.Equal("https://orcid.org/", cont.SchemaUri);
        Assert.Null(cont.Email);

        Assert.Single(cont.Role);
        RAiDContributorRole crole = cont.Role[0];
        Assert.Equal("https://credit.niso.org/contributor-roles/investigation/", crole.Id);
        Assert.Equal("https://credit.niso.org/", crole.SchemaUri);

        Assert.Single(cont.Position);
        RAiDContributorPosition pos = cont.Position[0];
        Assert.Equal("https://vocabulary.raid.org/contributor.position.schema/307", pos.Id);
        Assert.Equal("https://vocabulary.raid.org/contributor.position.schema/305", pos.SchemaUri);
        Assert.Equal(start, pos.StartDate);
        Assert.Null(pos.EndDate);

        Assert.True(cont.Leader);
        Assert.True(cont.Contact);

        Assert.NotNull(result.RelatedObject);
        Assert.Single(result.RelatedObject);
        RAiDRelatedObject relatedObject = result.RelatedObject[0];
        Assert.Equal("https://doi.org/10.5555/666655554444", relatedObject.Id);
        Assert.Equal("https://doi.org/", relatedObject.SchemaUri);
        Assert.NotNull(relatedObject.Type);
        Assert.Equal("https://vocabulary.raid.org/relatedObject.type.schema/250", relatedObject.Type.Id);
        Assert.Equal("https://vocabulary.raid.org/relatedObject.type.schema/329", relatedObject.Type.SchemaUri);

        Assert.NotNull(relatedObject.Category);
        Assert.Single(relatedObject.Category);
        RAiDRelatedObjectCategory objCat = relatedObject.Category[0];
        Assert.Equal("https://vocabulary.raid.org/relatedObject.category.schema/190", objCat.Id);
        Assert.Equal("https://vocabulary.raid.org/relatedObject.category.schema/386", objCat.SchemaUri);

        Assert.NotNull(result.Organisation);
        Assert.Single(result.Organisation);
        RAiDOrganisation org = result.Organisation[0];
        Assert.Equal("https://ror.org/04pp8hn57", org.Id);
        Assert.Equal("https://ror.org/", org.SchemaUri);
        Assert.Single(org.Role);
        RAiDOrganisationRole orgRole = org.Role[0];
        Assert.Equal("https://vocabulary.raid.org/organisation.role.schema/182", orgRole.Id);
        Assert.Equal("https://vocabulary.raid.org/organisation.role.schema/359", orgRole.SchemaUri);
        Assert.Equal(start, orgRole.StartDate);
        Assert.Null(orgRole.EndDate);

        Assert.NotNull(result.Title);
        Assert.Collection(result.Title, primaryTitle =>
            {
                Assert.Equal("Software Project Open-Science", primaryTitle.Text);
                Assert.NotNull(primaryTitle.Language);
                Assert.Equal("nld", primaryTitle.Language.Id);
                Assert.Equal("https://www.iso.org/standard/74575.html", primaryTitle.Language.SchemaUri);
                Assert.Equal("https://vocabulary.raid.org/title.type.schema/5", primaryTitle.Type.Id);
                Assert.Equal("https://vocabulary.raid.org/title.type.schema/376", primaryTitle.Type.SchemaUri);
                Assert.Equal(start, primaryTitle.StartDate);
                Assert.Null(primaryTitle.EndDate);
            },
            alternativeTitle =>
            {
                Assert.Equal("Conflux", alternativeTitle.Text);
                Assert.Null(alternativeTitle.Language);
                Assert.Equal("https://vocabulary.raid.org/title.type.schema/4", alternativeTitle.Type.Id);
                Assert.Equal("https://vocabulary.raid.org/title.type.schema/376", alternativeTitle.Type.SchemaUri);
                Assert.Equal(start, alternativeTitle.StartDate);
                Assert.Null(alternativeTitle.EndDate);
            });

        Assert.NotNull(result.Description);
        Assert.Single(result.Description);
        RAiDDescription desc = result.Description[0];

        Assert.Equal("Test", desc.Text);
        Assert.Equal("https://vocabulary.raid.org/description.type.schema/318", desc.Type.Id);
        Assert.Equal("https://vocabulary.raid.org/description.type.schema/320", desc.Type.SchemaUri);
        Assert.Null(desc.Language);
    }

    [Fact]
    public async Task MapProjectUpdateRequest_MapsProjectCorrectly()
    {
        Project project = await CreateProject();

        DateTime start = project.StartDate;

        RAiDUpdateRequest result = await _service.MapProjectUpdateRequest(project.Id);
        Assert.NotNull(result.Identifier);
        Assert.Equal("https://raid.org/10.0.0.0/375234214", result.Identifier.IdValue);
        Assert.Equal("https://raid.org/", result.Identifier.SchemaUri);
        Assert.Equal("Creative Commons CC-0", result.Identifier.License);
        Assert.Equal("https://ror.org/04pp8hn57", result.Identifier.Owner.Id);
        Assert.Equal("https://ror.org/", result.Identifier.Owner.SchemaUri);
        Assert.Equal(3, result.Identifier.Owner.ServicePoint);
        Assert.Equal(1, result.Identifier.Version);
        Assert.Equal("https://ror.org/009vhk114", result.Identifier.RegistrationAgency.Id);
        Assert.Equal("https://ror.org/", result.Identifier.RegistrationAgency.SchemaUri);

        Assert.NotNull(result.Date);
        Assert.Equal(start, result.Date.StartDate);
        Assert.Null(result.Date.EndDate);

        Assert.NotNull(result.Contributor);
        Assert.Single(result.Contributor);
        RAiDContributor cont = result.Contributor[0];
        Assert.Equal("https://orcid.org/0000-0002-1825-0097", cont.Id);
        Assert.Equal("https://orcid.org/", cont.SchemaUri);
        Assert.Null(cont.Email);

        Assert.Single(cont.Role);
        RAiDContributorRole crole = cont.Role[0];
        Assert.Equal("https://credit.niso.org/contributor-roles/investigation/", crole.Id);
        Assert.Equal("https://credit.niso.org/", crole.SchemaUri);

        Assert.Single(cont.Position);
        RAiDContributorPosition pos = cont.Position[0];
        Assert.Equal("https://vocabulary.raid.org/contributor.position.schema/307", pos.Id);
        Assert.Equal("https://vocabulary.raid.org/contributor.position.schema/305", pos.SchemaUri);
        Assert.Equal(start, pos.StartDate);
        Assert.Null(pos.EndDate);

        Assert.True(cont.Leader);
        Assert.True(cont.Contact);

        Assert.NotNull(result.RelatedObject);
        Assert.Single(result.RelatedObject);
        RAiDRelatedObject relatedObject = result.RelatedObject[0];
        Assert.Equal("https://doi.org/10.5555/666655554444", relatedObject.Id);
        Assert.Equal("https://doi.org/", relatedObject.SchemaUri);
        Assert.NotNull(relatedObject.Type);
        Assert.Equal("https://vocabulary.raid.org/relatedObject.type.schema/250", relatedObject.Type.Id);
        Assert.Equal("https://vocabulary.raid.org/relatedObject.type.schema/329", relatedObject.Type.SchemaUri);

        Assert.NotNull(relatedObject.Category);
        Assert.Single(relatedObject.Category);
        RAiDRelatedObjectCategory objCat = relatedObject.Category[0];
        Assert.Equal("https://vocabulary.raid.org/relatedObject.category.schema/190", objCat.Id);
        Assert.Equal("https://vocabulary.raid.org/relatedObject.category.schema/386", objCat.SchemaUri);

        Assert.NotNull(result.Organisation);
        Assert.Single(result.Organisation);
        RAiDOrganisation org = result.Organisation[0];
        Assert.Equal("https://ror.org/04pp8hn57", org.Id);
        Assert.Equal("https://ror.org/", org.SchemaUri);
        Assert.Single(org.Role);
        RAiDOrganisationRole orgRole = org.Role[0];
        Assert.Equal("https://vocabulary.raid.org/organisation.role.schema/182", orgRole.Id);
        Assert.Equal("https://vocabulary.raid.org/organisation.role.schema/359", orgRole.SchemaUri);
        Assert.Equal(start, orgRole.StartDate);
        Assert.Null(orgRole.EndDate);

        Assert.NotNull(result.Title);
        Assert.Collection(result.Title, primaryTitle =>
            {
                Assert.Equal("Software Project Open-Science", primaryTitle.Text);
                Assert.NotNull(primaryTitle.Language);
                Assert.Equal("nld", primaryTitle.Language.Id);
                Assert.Equal("https://www.iso.org/standard/74575.html", primaryTitle.Language.SchemaUri);
                Assert.Equal("https://vocabulary.raid.org/title.type.schema/5", primaryTitle.Type.Id);
                Assert.Equal("https://vocabulary.raid.org/title.type.schema/376", primaryTitle.Type.SchemaUri);
                Assert.Equal(start, primaryTitle.StartDate);
                Assert.Null(primaryTitle.EndDate);
            },
            alternativeTitle =>
            {
                Assert.Equal("Conflux", alternativeTitle.Text);
                Assert.Null(alternativeTitle.Language);
                Assert.Equal("https://vocabulary.raid.org/title.type.schema/4", alternativeTitle.Type.Id);
                Assert.Equal("https://vocabulary.raid.org/title.type.schema/376", alternativeTitle.Type.SchemaUri);
                Assert.Equal(start, alternativeTitle.StartDate);
                Assert.Null(alternativeTitle.EndDate);
            });

        Assert.NotNull(result.Description);
        Assert.Single(result.Description);
        RAiDDescription desc = result.Description[0];

        Assert.Equal("Test", desc.Text);
        Assert.Equal("https://vocabulary.raid.org/description.type.schema/318", desc.Type.Id);
        Assert.Equal("https://vocabulary.raid.org/description.type.schema/320", desc.Type.SchemaUri);
        Assert.Null(desc.Language);
    }

    private async Task<Project> CreateProject()
    {
        DateTime start = DateTime.UtcNow.Date.Subtract(TimeSpan.FromDays(10));
        
        Project project = new()
        {
            SCIMId = null,
            RAiDInfo = new()
            {
                LatestSync = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1)),
                Dirty = false,
                RAiDId = "https://raid.org/10.0.0.0/375234214",
                RegistrationAgencyId = "https://ror.org/009vhk114",
                OwnerId = "https://ror.org/04pp8hn57",
                OwnerServicePoint = 3,
                Version = 1,
            },
            StartDate = start,
            EndDate = null,
            Users = [],
            Contributors =
            [
                new()
                {
                    Person = new()
                    {
                        ORCiD = "https://orcid.org/0000-0002-1825-0097",
                        Name = "Josiah Carberry",
                        GivenName = "Josiah",
                        FamilyName = "Carberry",
                        Email = null,
                    },
                    Roles =
                    [
                        new()
                        {
                            RoleType = ContributorRoleType.Investigation,
                        },
                    ],
                    Positions =
                    [
                        new()
                        {
                            Position = ContributorPositionType.PrincipalInvestigator,
                            StartDate = start,
                            EndDate = null,
                        },
                    ],
                    Leader = true,
                    Contact = true,
                },
            ],
            Products =
            [
                new()
                {
                    Schema = ProductSchema.Doi,
                    Url = "https://doi.org/10.5555/666655554444",
                    Title = "The Memory Bus Considered Harmful",
                    Type = ProductType.JournalArticle,
                    Categories =
                    [
                        ProductCategoryType.Output,
                    ],
                },
            ],
            Organisations =
            [
                new()
                {
                    Organisation = new()
                    {
                        RORId = "https://ror.org/04pp8hn57",
                        Name = "Utrecht University",
                    },
                    Roles =
                    [
                        new()
                        {
                            Role = OrganisationRoleType.LeadResearchOrganization,
                            StartDate = start,
                            EndDate = null,
                        },
                    ],
                },
            ],
            Titles =
            [
                new()
                {
                    Text = "Software Project Open-Science",
                    Language = new()
                    {
                        Id = "nld",
                    },
                    Type = TitleType.Primary,
                    StartDate = start,
                    EndDate = null,
                },
                new()
                {
                    Text = "Conflux",
                    Language = null,
                    Type = TitleType.Alternative,
                    StartDate = start,
                    EndDate = null,
                },
            ],
            Descriptions =
            [
                new()
                {
                    Text = "Test",
                    Type = DescriptionType.Primary,
                    Language = null,
                },
            ],
            LastestEdit = DateTime.UtcNow,
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    [Fact]
    public async Task CheckProjectCompatibility_GivesNoIncompatibilities_WhenNoIncompatibilities()
    {
        Project project = await CreateProject();

        List<RAiDIncompatibility> result = await _service.CheckProjectCompatibility(project.Id);

        Assert.Empty(result);
    }

    [Fact]
    public async Task CheckProjectCompatibility_GivesIncompatibilities_WhenIncompatible()
    {
        DateTime start = DateTime.UtcNow.Date.Subtract(TimeSpan.FromDays(10));

        Project project = new()
        {
            SCIMId = null,
            RAiDInfo = new()
            {
                LatestSync = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1)),
                Dirty = false,
                RAiDId = "https://raid.org/10.0.0.0/375234214",
                RegistrationAgencyId = "https://ror.org/009vhk114",
                OwnerId = "https://ror.org/04pp8hn57",
                OwnerServicePoint = 3,
                Version = 1,
            },
            StartDate = start,
            EndDate = null,
            Users = [],
            Contributors =
            [
                new()
                {
                    Person = new()
                    {
                        ORCiD = "https://orcid.org/0000-0002-1825-0097",
                        Name = "Josiah Carberry",
                        GivenName = "Josiah",
                        FamilyName = "Carberry",
                        Email = null,
                    },
                    Roles =
                    [
                        new()
                        {
                            RoleType = ContributorRoleType.Investigation,
                        },
                    ],
                    Positions =
                    [
                        new()
                        {
                            Position = ContributorPositionType.PrincipalInvestigator,
                            StartDate = start,
                            EndDate = null,
                        },
                    ],
                    Leader = false,
                    Contact = false,
                },
                new()
                {
                    Person = new ()
                    {
                        ORCiD = null,
                        Name = "Geen echte Naam",
                        GivenName = "Geen",
                        FamilyName = "echte Naam",
                        Email = "g.echtenaam@xs4all.nl",
                    },
                    Roles = [
                        new()
                        {
                            RoleType = ContributorRoleType.Conceptualization
                        },
                    ],
                    Positions = [
                        new()
                        {
                            Position = ContributorPositionType.Consultant,
                            StartDate = start,
                            EndDate = DateTime.UtcNow.Date.Subtract(TimeSpan.FromDays(1)),
                        },
                        new()
                        {
                            Position = ContributorPositionType.Partner,
                            StartDate = DateTime.UtcNow.Date.Subtract(TimeSpan.FromDays(2)),
                            EndDate = null,
                        },
                    ],
                    Leader = false,
                    Contact = false,
                },
            ],
            Products =
            [
                new()
                {
                    Schema = ProductSchema.Doi,
                    Url = "https://doi.org/10.5555/666655554444",
                    Title = "The Memory Bus Considered Harmful",
                    Type = ProductType.JournalArticle,
                    Categories =
                    [
                    ],
                },
            ],
            Organisations =
            [
                new()
                {
                    Organisation = new()
                    {
                        RORId = "https://ror.org/04pp8hn57",
                        Name = "Utrecht University",
                    },
                    Roles =
                    [
                        new()
                        {
                            Role = OrganisationRoleType.LeadResearchOrganization,
                            StartDate = start,
                            EndDate = DateTime.UtcNow.Date.Subtract(TimeSpan.FromDays(1)),
                        },
                        new()
                        {
                            Role = OrganisationRoleType.Contractor,
                            StartDate = DateTime.UtcNow.Date.Subtract(TimeSpan.FromDays(2)),
                            EndDate = null,
                        },
                    ],
                },
                new ProjectOrganisation
                {
                    Organisation = new()
                    {
                        RORId = null,
                        Name = "Broodje Ben",
                    },
                    Roles = [
                        new()
                        {
                            Role = OrganisationRoleType.Funder,
                            StartDate = start,
                            EndDate = null,
                        },
                    ],
                },
            ],
            Titles =
            [
                new()
                {
                    Text = "Software Project Open-Science",
                    Language = new()
                    {
                        Id = "nld",
                    },
                    Type = TitleType.Primary,
                    StartDate = start,
                    EndDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1)),
                },
                new()
                {
                    Text = "Deze title is heeeel erg lang. Zelfs te lang om aan RAiD toe te voegen. Dat komt doordat titles op RAiD maar 100 karakters mogen zijn, maar deze titel is 173 karakters lang.",
                    Language = null,
                    Type = TitleType.Alternative,
                    StartDate = start,
                    EndDate = null,
                },
            ],
            Descriptions =
            [
                new()
                {
                    Text = "Met dank aan mijn kat Muis.",
                    Type = DescriptionType.Acknowledgements,
                    Language = null,
                },
                new()
                {
                    Text = string.Join('-', Enumerable.Repeat('0', 1000)),
                    Type = DescriptionType.Methods,
                    Language = null,
                },
            ],
            LastestEdit = DateTime.UtcNow,
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        List<RAiDIncompatibility> result = await _service.CheckProjectCompatibility(project.Id);
        
        Assert.Collection(result, [
            i => Assert.Equal(RAiDIncompatibilityType.NoActivePrimaryTitle, i.Type),
            i => Assert.Equal(RAiDIncompatibilityType.ProjectTitleTooLong, i.Type),
            i => Assert.Equal(RAiDIncompatibilityType.ProjectDescriptionTooLong, i.Type),
            i => Assert.Equal(RAiDIncompatibilityType.NoPrimaryDescription, i.Type),
            i => Assert.Equal(RAiDIncompatibilityType.ContributorWithoutOrcid, i.Type),
            i => Assert.Equal(RAiDIncompatibilityType.OverlappingContributorPositions, i.Type),
            i => Assert.Equal(RAiDIncompatibilityType.NoProjectLeader, i.Type),
            i => Assert.Equal(RAiDIncompatibilityType.NoProjectContact, i.Type),
            i => Assert.Equal(RAiDIncompatibilityType.OrganisationWithoutRor, i.Type),
            i => Assert.Equal(RAiDIncompatibilityType.OverlappingOrganisationRoles, i.Type),
            i => Assert.Equal(RAiDIncompatibilityType.NoLeadResearchOrganisation, i.Type),
            i => Assert.Equal(RAiDIncompatibilityType.NoProductCategory, i.Type),
        ]);
        
        

    }
}
