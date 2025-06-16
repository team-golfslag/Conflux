// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Security.Cryptography;
using System.Text;

namespace Conflux.Domain.Logic.Extensions;

/// <summary>
/// Helper methods for working with project embeddings
/// </summary>
public static class ProjectEmbeddingExtensions
{
    /// <summary>
    /// Generates a composite text representation of a project for embedding
    /// </summary>
    public static string GetEmbeddingText(this Project project)
    {
        var textParts = new List<string>();

        // Add primary titles
        var primaryTitles = project.Titles
            .Where(t => t.Type == TitleType.Primary && !string.IsNullOrWhiteSpace(t.Text))
            .Select(t => t.Text);
        textParts.AddRange(primaryTitles);

        // Add alternative titles
        var altTitles = project.Titles
            .Where(t => t.Type == TitleType.Alternative && !string.IsNullOrWhiteSpace(t.Text))
            .Select(t => t.Text);
        textParts.AddRange(altTitles);

        // Add primary descriptions
        var primaryDescriptions = project.Descriptions
            .Where(d => d.Type == DescriptionType.Primary && !string.IsNullOrWhiteSpace(d.Text))
            .Select(d => d.Text);
        textParts.AddRange(primaryDescriptions);

        // Add brief descriptions and objectives
        var additionalDescriptions = project.Descriptions
            .Where(d => (d.Type == DescriptionType.Brief || d.Type == DescriptionType.Objectives) 
                       && !string.IsNullOrWhiteSpace(d.Text))
            .Select(d => d.Text);
        textParts.AddRange(additionalDescriptions);

        return string.Join(" ", textParts);
    }

    /// <summary>
    /// Generates a hash of the embedding content to detect when re-embedding is needed
    /// </summary>
    public static string GetEmbeddingContentHash(this Project project)
    {
        var content = project.GetEmbeddingText();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash);
    }
}
