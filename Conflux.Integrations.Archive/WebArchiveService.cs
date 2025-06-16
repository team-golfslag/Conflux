// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Conflux.Integrations.Archive.Models;

namespace Conflux.Integrations.Archive;

public class WebArchiveService(HttpClient httpClient) : IWebArchiveService
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    private static readonly TimeSpan PollingTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    public async Task<string?> CreateArchiveLinkAsync(
        string urlToArchive) =>
        await TrySavePageAsync(urlToArchive);

    private async Task<string?> TrySavePageAsync(string urlToArchive)
    {
        HttpRequestMessage request = new(HttpMethod.Post, "https://web.archive.org/save")
        {
            Content = new FormUrlEncodedContent([
                new("url", urlToArchive),
                new("skip_first_archive", "1"),
            ]),
            Headers =
            {
                { "Accept", "application/json" },
                { "User-Agent", "Conflux Archive Integration" }
            }
        };
        try
        {
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.Found) 
                return response.Headers.Location?.AbsoluteUri;

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                Spn2SaveResponse? jobInfo = JsonSerializer.Deserialize<Spn2SaveResponse>(json);
                if (!string.IsNullOrEmpty(jobInfo?.JobId))
                {
                    return await PollJobStatusAsync(jobInfo.JobId);
                }
            }

            return null;
        }
        catch (HttpRequestException e)
        {
            throw new ArchiveException(
                $"Failed to save page {urlToArchive}. Error: {e.Message}", e);
        }
    }

    private async Task<string?> PollJobStatusAsync(string jobId)
    {
        string statusUrl = $"https://web.archive.org/save/status/{jobId}";
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < PollingTimeout)
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(statusUrl);
                if (!response.IsSuccessStatusCode)
                {
                    throw new ArchiveException(
                        $"Failed to poll job status for {jobId}. Status code: {response.StatusCode}");
                }

                string json = await response.Content.ReadAsStringAsync();
                Spn2SaveResponse? statusInfo = JsonSerializer.Deserialize<Spn2SaveResponse>(json);
                switch (statusInfo?.Status)
                {
                    case "success":
                        return $"https://web.archive.org/{statusInfo.Timestamp}/{statusInfo.OriginalUrl}";
                    case "pending":
                        await Task.Delay(PollingInterval);
                        continue;
                    default:
                        throw new ArchiveException(
                            $"Job {jobId} failed. Reason: {statusInfo?.Exception ?? statusInfo?.Status ?? "Unknown"}");
                }
            }
            catch (Exception ex)
            {
                throw new ArchiveException(
                    $"An error occurred while polling job status for {jobId}. Error: {ex.Message}", ex);
            }

        return null;
    }
}
