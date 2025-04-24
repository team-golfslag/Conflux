// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;

namespace Conflux.Integrations.NWOpen;

/// <summary>
/// Represents seed data.
/// </summary>
public class SeedData
{
    public List<Party> Parties { get; init; } = [];
    public List<Contributor> Contributors { get; init; } = [];
    public List<Product> Products { get; init; } = [];
    public List<Project> Projects { get; init; } = [];
}
