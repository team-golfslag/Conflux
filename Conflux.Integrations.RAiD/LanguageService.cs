// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Integrations.RAiD;

/// <summary>
/// A service to manage and validate ISO 639-3 language codes.
/// </summary>
public class LanguageService : ILanguageService
{
    private readonly HashSet<string> _languageCodes;
    private static readonly HttpClient HttpClient = new();

    /// <summary>
    /// Initializes a new instance of the LanguageService and populates the language codes.
    /// </summary>
    public LanguageService()
    {
        _languageCodes = new(StringComparer.OrdinalIgnoreCase);
        InitializeLanguagesAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously initializes the language codes from the ISO 639-3 source.
    /// </summary>
    private async Task InitializeLanguagesAsync()
    {
        const string url =
            "https://iso639-3.sil.org/sites/iso639-3/files/downloads/iso-639-3.tab";
        try
        {
            await using Stream stream = await HttpClient.GetStreamAsync(url);
            using StreamReader reader = new(stream);
            // Skip the header line
            await reader.ReadLineAsync();

            while (await reader.ReadLineAsync() is { } line)
            {
                string[] columns = line.Split('\t');
                if (columns.Length > 0 && !string.IsNullOrWhiteSpace(columns[0]))
                    _languageCodes.Add(columns[0]);
            }
        }
        catch (HttpRequestException e)
        {
            // Handle potential network errors or if the file is unavailable
            throw new InvalidOperationException(
                "Failed to download language data.",
                e
            );
        }
    }

    /// <summary>
    /// Retrieves an enumerable collection of all ISO 639-3 language codes.
    /// </summary>
    /// <returns>An IEnumerable of strings containing all language codes.</returns>
    public IEnumerable<string> GetAllLanguages() => _languageCodes;

    /// <summary>
    /// Checks if the given language code is a valid ISO 639-3 code.
    /// </summary>
    /// <param name="languageCode">The language code to validate.</param>
    /// <returns>True if the language code is valid; otherwise, false.</returns>
    public bool IsValidLanguageCode(string languageCode) => 
        languageCode.Length == 3 && _languageCodes.Contains(languageCode);
}
