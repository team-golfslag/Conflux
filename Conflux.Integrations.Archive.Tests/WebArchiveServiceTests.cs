// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Net;
using System.Text.Json;
using Conflux.Integrations.Archive.Models;
using Moq;
using Moq.Protected;

namespace Conflux.Integrations.Archive.Tests;

public class WebArchiveServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly WebArchiveService _webArchiveService;

    public WebArchiveServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _webArchiveService = new WebArchiveService(_httpClient);
    }

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new WebArchiveService(null!));
    }

    [Fact]
    public async Task CreateArchiveLinkAsync_HttpClientThrowsException_ThrowsArchiveException()
    {
        // Arrange
        const string testUrl = "https://example.com";
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.IsAny<HttpRequestMessage>(), 
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArchiveException>(
            () => _webArchiveService.CreateArchiveLinkAsync(testUrl));
        
        Assert.Contains("Failed to save page", exception.Message);
        Assert.Contains("Network error", exception.Message);
        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    [Fact]
    public async Task CreateArchiveLinkAsync_FoundResponse_ReturnsLocationHeader()
    {
        // Arrange
        const string testUrl = "https://example.com";
        const string archiveUrl = "https://web.archive.org/web/20231201000000/https://example.com";
        
        var response = new HttpResponseMessage(HttpStatusCode.Found);
        response.Headers.Location = new Uri(archiveUrl);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.RequestUri!.ToString().Contains("web.archive.org/save") &&
                    req.Method == HttpMethod.Post), 
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _webArchiveService.CreateArchiveLinkAsync(testUrl);

        // Assert
        Assert.Equal(archiveUrl, result);
    }

    [Fact]
    public async Task CreateArchiveLinkAsync_SuccessWithJobId_PollsAndReturnsArchiveUrl()
    {
        // Arrange
        const string testUrl = "https://example.com";
        const string jobId = "test-job-123";
        const string timestamp = "20231201000000";
        const string archiveUrl = $"https://web.archive.org/{timestamp}/{testUrl}";

        var saveResponse = new Spn2SaveResponse
        {
            JobId = jobId,
            Status = "pending"
        };

        var statusResponse = new Spn2SaveResponse
        {
            Status = "success",
            OriginalUrl = testUrl,
            Timestamp = timestamp
        };

        var saveHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(saveResponse))
        };

        var statusHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(statusResponse))
        };

        _mockHttpMessageHandler
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.IsAny<HttpRequestMessage>(), 
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(saveHttpResponse)
            .ReturnsAsync(statusHttpResponse);

        // Act
        var result = await _webArchiveService.CreateArchiveLinkAsync(testUrl);

        // Assert
        Assert.Equal(archiveUrl, result);
    }

    [Fact]
    public async Task CreateArchiveLinkAsync_JobStatusPending_RetriesUntilSuccess()
    {
        // Arrange
        const string testUrl = "https://example.com";
        const string jobId = "test-job-123";
        const string timestamp = "20231201000000";
        const string archiveUrl = $"https://web.archive.org/{timestamp}/{testUrl}";

        var saveResponse = new Spn2SaveResponse
        {
            JobId = jobId,
            Status = "pending"
        };

        var pendingStatusResponse = new Spn2SaveResponse
        {
            Status = "pending"
        };

        var successStatusResponse = new Spn2SaveResponse
        {
            Status = "success",
            OriginalUrl = testUrl,
            Timestamp = timestamp
        };

        var saveHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(saveResponse))
        };

        var pendingHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(pendingStatusResponse))
        };

        var successHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(successStatusResponse))
        };

        _mockHttpMessageHandler
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.IsAny<HttpRequestMessage>(), 
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(saveHttpResponse)
            .ReturnsAsync(pendingHttpResponse)
            .ReturnsAsync(successHttpResponse);

        // Act
        var result = await _webArchiveService.CreateArchiveLinkAsync(testUrl);

        // Assert
        Assert.Equal(archiveUrl, result);
    }

    [Fact]
    public async Task CreateArchiveLinkAsync_JobStatusFailed_ThrowsArchiveException()
    {
        // Arrange
        const string testUrl = "https://example.com";
        const string jobId = "test-job-123";
        const string errorMessage = "Archive failed";

        var saveResponse = new Spn2SaveResponse
        {
            JobId = jobId,
            Status = "pending"
        };

        var failedStatusResponse = new Spn2SaveResponse
        {
            Status = "failed",
            Exception = errorMessage
        };

        var saveHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(saveResponse))
        };

        var failedHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(failedStatusResponse))
        };

        _mockHttpMessageHandler
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.IsAny<HttpRequestMessage>(), 
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(saveHttpResponse)
            .ReturnsAsync(failedHttpResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArchiveException>(
            () => _webArchiveService.CreateArchiveLinkAsync(testUrl));
        
        Assert.Contains($"Job {jobId} failed", exception.Message);
        Assert.Contains(errorMessage, exception.Message);
    }

    [Fact]
    public async Task CreateArchiveLinkAsync_JobStatusUnknown_ThrowsArchiveException()
    {
        // Arrange
        const string testUrl = "https://example.com";
        const string jobId = "test-job-123";

        var saveResponse = new Spn2SaveResponse
        {
            JobId = jobId,
            Status = "pending"
        };

        var unknownStatusResponse = new Spn2SaveResponse
        {
            Status = "unknown"
        };

        var saveHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(saveResponse))
        };

        var unknownHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(unknownStatusResponse))
        };

        _mockHttpMessageHandler
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.IsAny<HttpRequestMessage>(), 
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(saveHttpResponse)
            .ReturnsAsync(unknownHttpResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArchiveException>(
            () => _webArchiveService.CreateArchiveLinkAsync(testUrl));
        
        Assert.Contains($"Job {jobId} failed", exception.Message);
        Assert.Contains("unknown", exception.Message);
    }

    [Fact]
    public async Task CreateArchiveLinkAsync_JobStatusPollingFails_ThrowsArchiveException()
    {
        // Arrange
        const string testUrl = "https://example.com";
        const string jobId = "test-job-123";

        var saveResponse = new Spn2SaveResponse
        {
            JobId = jobId,
            Status = "pending"
        };

        var saveHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(saveResponse))
        };

        var failedStatusResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        _mockHttpMessageHandler
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.IsAny<HttpRequestMessage>(), 
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(saveHttpResponse)
            .ReturnsAsync(failedStatusResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArchiveException>(
            () => _webArchiveService.CreateArchiveLinkAsync(testUrl));
        
        Assert.Contains($"Failed to poll job status for {jobId}", exception.Message);
        Assert.Contains("InternalServerError", exception.Message);
    }

    [Fact]
    public async Task CreateArchiveLinkAsync_NonSuccessStatusCode_ReturnsNull()
    {
        // Arrange
        const string testUrl = "https://example.com";
        
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.IsAny<HttpRequestMessage>(), 
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _webArchiveService.CreateArchiveLinkAsync(testUrl);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateArchiveLinkAsync_SuccessWithoutJobId_ReturnsNull()
    {
        // Arrange
        const string testUrl = "https://example.com";
        
        var saveResponse = new Spn2SaveResponse
        {
            JobId = null,
            Status = "success"
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(saveResponse))
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.IsAny<HttpRequestMessage>(), 
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _webArchiveService.CreateArchiveLinkAsync(testUrl);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateArchiveLinkAsync_SuccessWithEmptyJobId_ReturnsNull()
    {
        // Arrange
        const string testUrl = "https://example.com";
        
        var saveResponse = new Spn2SaveResponse
        {
            JobId = string.Empty,
            Status = "success"
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(saveResponse))
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.IsAny<HttpRequestMessage>(), 
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _webArchiveService.CreateArchiveLinkAsync(testUrl);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateArchiveLinkAsync_ValidatesRequestParameters()
    {
        // Arrange
        const string testUrl = "https://example.com";
        HttpRequestMessage? capturedRequest = null;
        
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.IsAny<HttpRequestMessage>(), 
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

        // Act
        await _webArchiveService.CreateArchiveLinkAsync(testUrl);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("https://web.archive.org/save", capturedRequest.RequestUri?.ToString());
        Assert.Contains("Accept", capturedRequest.Headers.Select(h => h.Key));
        Assert.Contains("User-Agent", capturedRequest.Headers.Select(h => h.Key));
        Assert.Equal("application/json", capturedRequest.Headers.Accept.First().MediaType);
        Assert.Equal("Conflux Archive Integration", capturedRequest.Headers.UserAgent.ToString());
        
        // Verify form content
        Assert.IsType<FormUrlEncodedContent>(capturedRequest.Content);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
