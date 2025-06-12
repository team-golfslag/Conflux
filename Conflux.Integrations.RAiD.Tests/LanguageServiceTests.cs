// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
//
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Net;
using System.Text;
using Xunit;

namespace Conflux.Integrations.RAiD.Tests;

/// <summary>
/// Test class for the LanguageService.
/// </summary>
public class LanguageServiceTests
{
    /// <summary>
    /// Provides mock tab-separated data for testing purposes.
    /// Includes a header, valid data, data with insufficient columns,
    /// and data with blank fields.
    /// </summary>
    private const string MockLanguageData =
        "Id\tPart2B\tPart2T\tPart1\tScope\tLanguage_Type\tRef_Name\tComment\n" + // Header
        "eng\t\t\ten\tI\tL\tEnglish\t\n" + // Valid entry
        "spa\t\t\tes\tI\tL\tSpanish\t\n" + // Valid entry
        "fra\t\t\tfr\tI\tL\tFrench\t\n" + // Valid entry
        "zxx\t\t\t\tS\tS\tNo linguistic content\t\n" + // Valid entry
        "invalid\ttoo\tfew\tcolumns\n" + // Invalid entry (too few columns)
        "   \t\t\t\tI\tL\t\t\n" + // Invalid entry (blank Id)
        "abc\t\t\t\tI\tL\t   \t\n"; // Invalid entry (blank Ref_Name)
    

    [Theory]
    [InlineData("eng", true)] // Valid code
    [InlineData("spa", true)] // Valid code
    [InlineData("EnG", true)] // Valid code, different case (tests OrdinalIgnoreCase)
    [InlineData("zzz", false)] // Invalid 3-letter code
    [InlineData("en", false)] // Code too short
    [InlineData("engl", false)] // Code too long
    [InlineData("", false)] // Empty string
    [InlineData(null, false)] // Null value
    public async Task IsValidLanguageCode_WithVariousInputs_ReturnsExpectedResult(
        string code,
        bool expected
    )
    {
        // Arrange
        MockHttpMessageHandler mockHttp = new(
            MockLanguageData,
            HttpStatusCode.OK
        );
        HttpClient httpClient = new(mockHttp);
        LanguageService languageService = new(httpClient);

        // Act
        bool result = languageService.IsValidLanguageCode(code);

        // Assert
        Assert.Equal(expected, result);
    }
}

public class MockHttpMessageHandler(string content, HttpStatusCode statusCode, Exception? exceptionToThrow = null) : HttpMessageHandler
{
    private readonly string? _content = content;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (exceptionToThrow != null) throw exceptionToThrow;

        HttpResponseMessage response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(_content ?? "", Encoding.UTF8)
        };

        return Task.FromResult(response);
    }
}