// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using Product = Conflux.Domain.Product;
using Project = Conflux.Domain.Project;
using NwOpenProject = NWOpen.Net.Models.Project;
using NwOpenProduct = NWOpen.Net.Models.Product;
using NwOpenProjectMember = NWOpen.Net.Models.ProjectMember;

namespace Conflux.RepositoryConnections.NWOpen;

public static class NwOpenMapper
{
    private static List<Party> Parties { get; } = [];
    private static List<Contributor> Contributors { get; } = [];
    private static List<Product> Products { get; } = [];
    private static List<Project> Projects { get; } = [];

    /// <summary>
    /// Maps a list of NWOpen projects to domain projects
    /// </summary>
    /// <param name="projects">The list of NWOpen projects to be mapped</param>
    /// <returns>A <see cref="SeedData" /> object with the mapped projects and their connected people, products, and parties</returns>
    public static SeedData MapProjects(List<NwOpenProject> projects)
    {
        foreach (NwOpenProject project in projects) MapProject(project);

        return new()
        {
            Parties = Parties,
            Contributors = Contributors,
            Products = Products,
            Projects = Projects,
        };
    }

    /// <summary>
    /// Maps an NWOpen project to a domain project.
    /// </summary>
    /// <param name="project">The NWOpen project to map</param>
    private static void MapProject(NwOpenProject project)
    {
        DateTime? startDate = project.StartDate.HasValue
            ? DateTime.SpecifyKind(project.StartDate.Value, DateTimeKind.Utc)
            : null;

        DateTime? endDate = project.EndDate.HasValue
            ? DateTime.SpecifyKind(project.EndDate.Value, DateTimeKind.Utc)
            : null;


        Project mappedProject = new()
        {
            Title = project.Title!,
            Description = project.SummaryNl,
            StartDate = startDate,
            EndDate = endDate,
            SCIMId = "SCIM",
        };

        foreach (NwOpenProduct product in project.Products ?? []) MapProduct(mappedProject, product);

        foreach (NwOpenProjectMember projectMember in project.ProjectMembers ?? [])
        {
            MapContributor(mappedProject, projectMember);
            MapParty(mappedProject, projectMember);
        }

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

        Product mappedProduct = new()
        {
            Id = Guid.NewGuid(),
            Title = product.Title ?? "No title",
            Url = product.UrlOpenAccess,
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
        Contributor contributor = new()
        {
            Id = Guid.NewGuid(),
            Name = $"{projectMember.FirstName} {projectMember.LastName}",
        };
        Contributors.Add(contributor);
        project.Contributors.Add(contributor);
    }

    /// <summary>
    /// Maps a project member's organisation to a party.
    /// </summary>
    /// <param name="project">The project to which the party is added</param>
    /// <param name="projectMember">The member from which the party is retrieved</param>
    private static void MapParty(Project project, NwOpenProjectMember projectMember)
    {
        var parties = Parties.Where(p => p.Name == projectMember.Organisation).ToList();
        if (parties.Count != 0)
        {
            project.Parties.Add(parties[0]);
            return;
        }

        Party mappedParty = new()
        {
            Id = Guid.NewGuid(),
            Name = projectMember.Organisation!,
        };

        project.Parties.Add(mappedParty);
        Parties.Add(mappedParty);
    }
}
