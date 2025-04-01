using System.Text.RegularExpressions;
using Conflux.Domain;
using DoiTools.Net;
using Product = Conflux.Domain.Product;
using Project = Conflux.Domain.Project;
using NwOpenProject = NWOpen.Net.Models.Project;
using NwOpenProduct = NWOpen.Net.Models.Product;
using NwOpenProjectMember = NWOpen.Net.Models.ProjectMember;

namespace Conflux.RepositoryConnections.NWOpen;

public static class NwOpenMapper
{
    private static List<Party> Parties { get; } = [];
    private static List<Person> People { get; } = [];
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
            People = People,
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
            Title = project.Title,
            Description = project.SummaryNl,
            StartDate = startDate,
            EndDate = endDate,
        };

        foreach (NwOpenProduct product in project.Products ?? []) MapProduct(mappedProject, product);

        foreach (NwOpenProjectMember projectMember in project.ProjectMembers ?? [])
        {
            MapPerson(mappedProject, projectMember);
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
            IsValidUrl = CheckIsValidUrl(product.UrlOpenAccess),
        };

        project.Products.Add(mappedProduct);
        Products.Add(mappedProduct);
    }
    
    /// <summary>
    /// given a url, checks if it is a valid DOI or http(s) url.
    /// </summary>
    /// <param name="url"> nullable url to check if it is valid</param>
    /// <returns>bool</returns>
    private static bool CheckIsValidUrl(string? url)
    {
        if(string.IsNullOrEmpty(url)) return false;
        
        if(Doi.IsValid(url)) return true; //check if it is a valid DOI url
        
        // check if it a valid http(s) url.
        var isMatch = Regex.Match(url, @"(?:http[s]?:\/\/.)?(?:www\.)?[-a-zA-Z0-9@%._\+~#=]{2,256}\.[a-z]{2,6}\b(?:[-a-zA-Z0-9@:%_\+.~#?&\/\/=]*)");
        if (isMatch.Success) return true;
        return false;
    }

    /// <summary>
    /// Maps a project member to a person.
    /// </summary>
    /// <param name="project">The project to which the person is added</param>
    /// <param name="projectMember">The member to map to a person</param>
    private static void MapPerson(Project project, NwOpenProjectMember projectMember)
    {
        Person person = new()
        {
            Id = Guid.NewGuid(),
            Name = $"{projectMember.FirstName} {projectMember.LastName}",
        };
        People.Add(person);
        project.People.Add(person);
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
            Name = projectMember.Organisation,
        };

        project.Parties.Add(mappedParty);
        Parties.Add(mappedParty);
    }
}
