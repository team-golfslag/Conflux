// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Net;

namespace Conflux.Integrations.SRAM.Tests.Helpers;

/// <summary>
/// A fake HTTP message handler for testing purposes.
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly string _responseContent;
    private readonly HttpStatusCode _statusCode;

    public FakeHttpMessageHandler(string responseContent, HttpStatusCode statusCode)
    {
        _responseContent = responseContent;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response = new(_statusCode)
        {
            Content = new StringContent(_responseContent),
        };
        return Task.FromResult(response);
    }
}
