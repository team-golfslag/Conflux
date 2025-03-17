using Conflux.Domain;
using Product = Conflux.Domain.Product;
using Project = Conflux.Domain.Project;
using NwOpenProject = NWOpen.Net.Models.Project;
using NwOpenProduct = NWOpen.Net.Models.Product;
using NwOpenProjectMember = NWOpen.Net.Models.ProjectMember;

namespace Conflux.RepositoryConnections.NWOpen;

public static class NwOpenMapper
{
    /// <summary>
    /// Maps an NWOpen project to a domain project.
    /// </summary>
    /// <param name="project">The NWOpen project to map</param>
    /// <returns>The mapped domain project</returns>
    public static Project MapProject(NwOpenProject project) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = project.Title,
            Description = project.SummaryNl,
            People = (project.ProjectMembers ?? []).Select(MapPerson).ToList(),
            Products = (project.Products ?? []).Aggregate(new List<Product>(), MapProduct).ToList(),
            Parties = (project.ProjectMembers ?? []).Aggregate(new HashSet<Party>(), MapParty).ToList(),
        };

    /// <summary>
    /// Maps an NWOpen product to a domain product.
    /// </summary>
    /// <param name="products">The domain products to which the product is added</param>
    /// <param name="product">The NWOpen product to map</param>
    /// <returns>The mapped domain product</returns>
    private static List<Product> MapProduct(List<Product> products, NwOpenProduct product)
    {
        if (products.Any(p => p.Title == product.Title))
        {
            return products;
        }

        products.Add(
            new()
            {
                Title = product.Title!,
                Url = product.UrlOpenAccess ?? "",
            });
        return products;
    }


    /// <summary>
    /// Maps a project member to a person.
    /// </summary>
    /// <param name="projectMember">The member to map to a person</param>
    /// <returns>The mapped Person</returns>
    private static Person MapPerson(NwOpenProjectMember projectMember) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = $"{projectMember.FirstName} {projectMember.LastName}",
        };

    /// <summary>
    /// Maps a project member's organisation to a party.
    /// </summary>
    /// <param name="parties">The parties to which the party is added</param>
    /// <param name="projectMember">The member from which the party is retrieved</param>
    /// <returns>The mapped Party</returns>
    private static HashSet<Party> MapParty(HashSet<Party> parties, NwOpenProjectMember projectMember)
    {
        parties.Add(new()
        {
            Id = Guid.NewGuid(),
            Name = projectMember.Organisation,
        });
        return parties;
    }
}
