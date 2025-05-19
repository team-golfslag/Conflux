// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using Conflux.Domain.Session;
using Product = Conflux.Domain.Product;
using Project = Conflux.Domain.Project;
using NwOpenProject = NWOpen.Net.Models.Project;
using NwOpenProduct = NWOpen.Net.Models.Product;
using NwOpenProjectMember = NWOpen.Net.Models.ProjectMember;

namespace Conflux.Integrations.NWOpen;

public static class NwOpenMapper
{
    private static List<Organisation> Organisations { get; } = [];
    private static List<Contributor> Contributors { get; } = [];
    private static List<Person> People { get; } = [];
    private static List<Product> Products { get; } = [];
    private static List<Project> Projects { get; } = [];
    private static List<User> Users { get; } = [];
    private static List<UserRole> UserRoles { get; } = [];

    /// <summary>
    /// Maps a list of NWOpen projects to domain projects
    /// </summary>
    /// <param name="projects">The list of NWOpen projects to be mapped</param>
    /// <returns>
    /// A <see cref="SeedData" /> object with the mapped projects and their connected people, products, and
    /// organisations
    /// </returns>
    public static SeedData MapProjects(List<NwOpenProject> projects)
    {
        foreach (NwOpenProject project in projects) MapProject(project);

        return new()
        {
            Organisations = Organisations,
            Contributors = Contributors,
            People = People,
            Products = Products,
            Projects = Projects,
            Users = Users,
            UserRoles = UserRoles,
        };
    }

    /// <summary>
    /// Maps an NWOpen project to a domain project.
    /// </summary>
    /// <param name="project">The NWOpen project to map</param>
    private static void MapProject(NwOpenProject project)
    {
        DateTime startDate = project.StartDate.HasValue
            ? DateTime.SpecifyKind(project.StartDate.Value, DateTimeKind.Utc)
            : DateTime.UtcNow;

        DateTime? endDate = project.EndDate.HasValue
            ? DateTime.SpecifyKind(project.EndDate.Value, DateTimeKind.Utc)
            : null;

        Guid projectId = Guid.NewGuid();

        List<ProjectDescription> descriptions = new();
        if (project.SummaryNl != null)
            descriptions.Add(new()
            {
                ProjectId = projectId,
                Text = project.SummaryNl,
                Type = DescriptionType.Primary,
                Language = Language.DUTCH,
            });
        if (project.SummaryEn != null)
            descriptions.Add(new()
            {
                ProjectId = projectId,
                Text = project.SummaryEn,
                Language = Language.ENGLISH,
                Type = DescriptionType.Alternative,
            });

        Project mappedProject = new()
        {
            Id = projectId,
            Titles =
            [
                new()
                {
                    ProjectId = projectId,
                    Text = project.Title ?? "No Title",
                    Type = TitleType.Primary,
                    Language = Language.DUTCH,
                    StartDate = startDate,
                    EndDate = endDate,
                },
            ],
            Descriptions = descriptions,
            StartDate = startDate,
            EndDate = endDate,
            SCIMId = "SCIM",
        };

        foreach (NwOpenProduct product in project.Products ?? []) MapProduct(mappedProject, product);

        foreach (NwOpenProjectMember projectMember in project.ProjectMembers ?? [])
        {
            MapContributor(mappedProject, projectMember);
            MapOrganisation(mappedProject, projectMember);
        }

        // Add development user to the project with Admin and User roles
        AddDevelopmentUserToProject(mappedProject);

        Projects.Add(mappedProject);
    }

    /// <summary>
    /// Maps an NWOpen product to a domain product.
    /// </summary>
    /// <param name="project">The project to which the product is added</param>
    /// <param name="product">The NWOpen product to map</param>
    private static void MapProduct(Project project, NwOpenProduct product)
    {
        var products = Products.Where(p => p.Url == product.UrlOpenAccess).ToList();
        if (products.Count != 0)
        {
            project.Products.Add(products[0]);
            return;
        }

        Guid productId = Guid.NewGuid();

        Product mappedProduct = new()
        {
            Id = productId,
            Title = product.Title ?? "No title",
            Schema = ProductSchema.Doi,
            Url = product.UrlOpenAccess,
            Type = ProductType.DataPaper,
            Categories =
            [
                new()
                {
                    ProductId = productId,
                    Type = ProductCategoryType.Input,
                },
            ],
        };

        project.Products.Add(mappedProduct);
        Products.Add(mappedProduct);
    }

    /// <summary>
    /// Maps a project member to a contributor.
    /// </summary>
    /// <param name="project">The project to which the contributor is added</param>
    /// <param name="projectMember">The member to map to a person</param>
    private static void MapContributor(Project project, NwOpenProjectMember projectMember)
    {
        Guid personId = Guid.NewGuid();
        Person person = new()
        {
            Id = personId,
            Name = $"{projectMember.FirstName} {projectMember.LastName}",
            GivenName = projectMember.FirstName,
            FamilyName = projectMember.LastName,
            ORCiD = "https://orcid.org/0009-0003-2462-3499",
        };
        Contributor contributor = new()
        {
            PersonId = personId,
            ProjectId = project.Id,
            Roles =
            [
                new()
                {
                    PersonId = personId,
                    ProjectId = project.Id,
                    RoleType = ContributorRoleType.Conceptualization,
                },
                new()
                {
                    PersonId = personId,
                    ProjectId = project.Id,
                    RoleType = ContributorRoleType.Methodology,
                },
                new()
                {
                    PersonId = personId,
                    ProjectId = project.Id,
                    RoleType = ContributorRoleType.Validation,
                },
            ],
            Positions =
            [
                new()
                {
                    PersonId = personId,
                    ProjectId = project.Id,
                    Position = ContributorPositionType.CoInvestigator,
                    StartDate = project.StartDate,
                    EndDate = project.EndDate,
                },
            ],
            Leader = true,
            Contact = true,
        };
        People.Add(person);
        Contributors.Add(contributor);
        project.Contributors.Add(contributor);
    }

    /// <summary>
    /// Maps a project member's organisation to a organisation.
    /// </summary>
    /// <param name="project">The project to which the organisation is added</param>
    /// <param name="projectMember">The member from which the organisation is retrieved</param>
    private static void MapOrganisation(Project project, NwOpenProjectMember projectMember)
    {
        var organisations = Organisations.Where(p => p.Name == projectMember.Organisation).ToList();
        if (organisations.Count != 0)
        {
            project.Organisations.Add(organisations[0]);
            return;
        }

        Guid organisationId = Guid.NewGuid();

        Organisation mappedOrganisation = new()
        {
            Id = organisationId,
            RORId = "https://ror.org/04pp8hn57",
            Name = projectMember.Organisation!,
            Roles =
            [
                new()
                {
                    OrganisationId = organisationId,
                    Role = OrganisationRoleType.Contractor,
                    StartDate = project.StartDate,
                    EndDate = project.EndDate,
                },
                new()
                {
                    OrganisationId = Guid.NewGuid(),
                    Role = OrganisationRoleType.Funder,
                    StartDate = project.StartDate,
                    EndDate = project.EndDate,
                },
                new()
                {
                    OrganisationId = Guid.NewGuid(),
                    Role = OrganisationRoleType.Facility,
                    StartDate = project.StartDate,
                    EndDate = project.EndDate,
                },
            ],
        };

        project.Organisations.Add(mappedOrganisation);
        Organisations.Add(mappedOrganisation);
    }

    /// <summary>
    /// Adds the development user to a project with Admin and User roles.
    /// </summary>
    /// <param name="project">The project to add the user to</param>
    private static void AddDevelopmentUserToProject(Project project)
    {
        // Get the development user
        User devUser;
        
        // Check if development user already exists in our list
        User? existingUser = Users.FirstOrDefault(u => u.Id == UserSession.DevelopmentUserId);
        if (existingUser != null)
        {
            // Use the existing user from our list
            devUser = existingUser;
        }
        else
        {
            // Create a new development user and add to our list
            devUser = UserSession.Development().User!;
            Users.Add(devUser);
        }

        // Add user to project
        project.Users.Add(devUser);
        
        // Create Admin role for the user
        UserRole adminRole = new()
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Type = UserRoleType.Admin,
            Urn = "urn:mace:surf.nl:sram:group:surf:development:conflux-cx_project_admin",
            SCIMId = devUser.SCIMId
        };
        
        // Create User role for the user
        UserRole userRole = new()
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Type = UserRoleType.User,
            Urn = "urn:mace:surf.nl:sram:group:surf:development:conflux-cx_project_user",
            SCIMId = devUser.SCIMId
        };
        
        // Add roles to the lists
        UserRoles.Add(adminRole);
        UserRoles.Add(userRole);
        
        // Add roles to the user
        devUser.Roles.Add(adminRole);
        devUser.Roles.Add(userRole);
    }
}
