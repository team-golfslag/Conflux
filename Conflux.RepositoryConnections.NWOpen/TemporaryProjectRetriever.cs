// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using NWOpen.Net.Models;
using NWOpen.Net.Services;

namespace Conflux.RepositoryConnections.NWOpen;

/// <summary>
/// Maps NWOpen data to domain objects. Singleton class.
/// </summary>
public class TemporaryProjectRetriever
{
    private static TemporaryProjectRetriever? _instance;

    private static readonly Lock Lock = new();

    private readonly NWOpenService _service;

    /// <summary>
    /// Constructs the <see cref="TemporaryProjectRetriever" />
    /// </summary>
    private TemporaryProjectRetriever()
    {
        _service = new(new(new HttpClientHandler()));
    }

    /// <summary>
    /// Gets the instance of the <see cref="TemporaryProjectRetriever" />
    /// </summary>
    /// <returns>The instance of the <see cref="TemporaryProjectRetriever" /></returns>
    public static TemporaryProjectRetriever GetInstance()
    {
        if (_instance != null) return _instance;
        lock (Lock)
        {
            _instance ??= new();
        }

        return _instance;
    }

    /// <summary>
    /// Maps NWOpen projects to domain projects.
    /// </summary>
    /// <param name="numberOfResults">The number of results to map</param>
    /// <returns>The mapped projects</returns>
    public Task<SeedData> MapProjectsAsync(int numberOfResults = 100)
    {
        NWOpenResult? result = _service.Query().WithNumberOfResults(numberOfResults).Execute().Result;
        if (result == null) return Task.FromResult(new SeedData());

        SeedData projects = NwOpenMapper.MapProjects(result.Projects);

        return Task.FromResult(projects);
    }
}
