﻿using Microsoft.Extensions.Logging;
using NWOpen.Net.Models;
using NWOpen.Net.Services;
using Project = Conflux.Domain.Project;

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
        _service = new(new(new HttpClientHandler()), new Logger<NWOpenService>(new LoggerFactory()));
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
    public Task<List<Project>> MapProjectsAsync(int numberOfResults = 100)
    {
        NWOpenResult? result = _service.Query().WithNumberOfResults(numberOfResults).Execute().Result;
        if (result == null) return Task.FromResult(new List<Project>());

        var projects = result.Projects!.Select(NwOpenMapper.MapProject).ToList();

        return Task.FromResult(projects);
    }
}
