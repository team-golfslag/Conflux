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
    private readonly Dictionary<string, string> _languageCodes;
    private readonly HttpClient _httpClient;

    /// <summary>
    // Initializes a new instance of the LanguageService and populates the language codes.
    /// </summary>
    public LanguageService() : this(new()) { }

    // Add a new constructor for dependency injection (and testing)
    /// <summary>
    /// Initializes a new instance of the LanguageService with a specific HttpClient.
    /// </summary>
    /// <param name="httpClient">The HttpClient to use for requests.</param>
    public LanguageService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _languageCodes = new(StringComparer.OrdinalIgnoreCase);
        InitializeLanguagesAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously initializes the language codes from the ISO 639-3 source.
    /// </summary>
    public async Task InitializeLanguagesAsync()
    {
        const string url =
            "https://iso639-3.sil.org/sites/iso639-3/files/downloads/iso-639-3.tab";
        try
        {
            await using Stream stream = await _httpClient.GetStreamAsync(url);
            using StreamReader reader = new(stream);
            // Skip the header line
            await reader.ReadLineAsync();

            while (await reader.ReadLineAsync() is { } line)
            {
                string[] columns = line.Split('\t');

                // The 'Id' is at index 0 and 'Ref_Name' is at index 6.
                // Ensure the line has enough columns to safely access index 6.
                if (columns.Length <= 6)
                    continue;

                string id = columns[0];
                string refName = columns[6];

                // Ensure both the key (id) and value (refName) are valid
                if (!string.IsNullOrWhiteSpace(id) &&
                    !string.IsNullOrWhiteSpace(refName))
                    _languageCodes[id] = refName;
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
    public Dictionary<string, string> GetAllLanguages() => _languageCodes;

    /// <summary>
    /// Checks if the given language code is a valid ISO 639-3 code.
    /// </summary>
    /// <param name="languageCode">The language code to validate.</param>
    /// <returns>True if the language code is valid; otherwise, false.</returns>
    public bool IsValidLanguageCode(string languageCode) =>
        !string.IsNullOrEmpty(languageCode) &&
        languageCode.Length == 3 &&
        _languageCodes.ContainsKey(languageCode);
}