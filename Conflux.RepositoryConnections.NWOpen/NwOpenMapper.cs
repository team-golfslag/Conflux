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
    /// <returns>The mapped domain project</returns>
    private static void MapProject(NwOpenProject project)
    {
        Project mappedProject = new()
        {
            Title = project.Title,
            Description = project.SummaryNl,
            StartDate = DateTimeToDateOnly(project.StartDate),
            EndDate = DateTimeToDateOnly(project.EndDate),
        };

        foreach (NwOpenProduct product in project.Products ?? []) MapProduct(mappedProject, product);

        foreach (NwOpenProjectMember projectMember in project.ProjectMembers ?? [])
        {
            MapPerson(mappedProject, projectMember);
            MapParty(mappedProject, projectMember);
        }

        Projects.Add(mappedProject);
    }

    private static DateOnly? DateTimeToDateOnly(DateTime? dateTime) =>
        dateTime == null ? null : DateOnly.FromDateTime(dateTime.Value);


    /// <summary>
    /// Maps an NWOpen product to a domain product.
    /// </summary>
    /// <param name="project">The project to which the product is added</param>
    /// <param name="product">The NWOpen product to map</param>
    /// <returns>The mapped domain product</returns>
    private static void MapProduct(Project project, NwOpenProduct product)
    {
        var products = Products.Where(p => p.Url == product.UrlOpenAccess).ToList();
        if (products.Count != 0)
        {
            project.Products.Add(products[0]);
            return;
        }

        var (productUrl, isValid) = CheckIsValidUrl(product.UrlOpenAccess);
        Product mappedProduct = new()
        {
            Id = Guid.NewGuid(),
            Title = product.Title ?? "No title",
            Url = productUrl,
            IsValidUrl = isValid,
        };
        
        project.Products.Add(mappedProduct);
        Products.Add(mappedProduct);
    }
    
    /// <summary>
    /// checks if a url is a doi and if it leads to an existing web page. It converts the url to a standard doi url and if the url is valid.
    /// </summary>
    /// <param name="url"> a nullable string to a doi or an HTTP url</param>
    /// <returns> a tuple of a corrected url and a isValidUrl bool</returns>
    private static (string?, bool) CheckIsValidUrl(string? url)
    {

        if (string.IsNullOrEmpty(url)) return (url, false);
        
        HttpClient client = new();
        if (url.StartsWith("doi:")) // case 1: is a doi.
        {
            url = Regex.Replace(url, @"doi:[ ]?", "https://doi.org/"); //some urls have a trailing space.
             
            bool isValid = Doi.TryParse(url, out Doi? doi);
            if (isValid)
            {
                return ("https://doi.org/" + doi!.ToString(), isValid);
            }
            return (url, false);
        }
        
        try //case 2: not a doi, but may still be valid. This checks for the OK, not for the body.
        {
            var response = client.GetAsync(url);
            response.Wait();
            response.Result.EnsureSuccessStatusCode(); // throws error if not OK
            return (url, true);
        }
        catch
        {
            return (url, false);
        }
    }

    /// <summary>
    /// Maps a project member to a person.
    /// </summary>
    /// <param name="project">The project to which the person is added</param>
    /// <param name="projectMember">The member to map to a person</param>
    /// <returns>The mapped Person</returns>
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
    /// <returns>The mapped Party</returns>
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
