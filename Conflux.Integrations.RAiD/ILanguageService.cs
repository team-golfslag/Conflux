// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Integrations.RAiD;

/// <summary>
/// Defines the contract for a service that provides language code information.
/// </summary>
public interface ILanguageService
{
    /// <summary>
    /// Retrieves an enumerable collection of all ISO 639-3 language codes.
    /// </summary>
    /// <returns>An IEnumerable of strings containing all language codes.</returns>
    IEnumerable<string> GetAllLanguages();

    /// <summary>
    /// Checks if the given language code is a valid ISO 639-3 code.
    /// </summary>
    /// <param name="languageCode">The language code to validate.</param>
    /// <returns>True if the language code is valid; otherwise, false.</returns>
    bool IsValidLanguageCode(string languageCode);
}