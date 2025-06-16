// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Integrations.Archive;

/// <summary>
/// Defines a service for creating web archive links using the
/// Internet Archive.
/// </summary>
public interface IWebArchiveService
{
    /// <summary>
    /// Requests a URL to be archived. If the save fails, it can fall back
    /// to finding the latest existing archive.
    /// </summary>
    /// <param name="urlToArchive">The public URL to archive.</param>
    /// <returns>The URL of the archive, or null if all operations fail.</returns>
    Task<string?> CreateArchiveLinkAsync(
        string urlToArchive);
}