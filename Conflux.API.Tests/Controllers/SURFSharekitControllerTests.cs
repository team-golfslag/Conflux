// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Conflux.Data;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.DTOs.Patch;
using SURFSharekit.Net.Models.Webhooks;
using Xunit;
using IServiceScope = Microsoft.Extensions.DependencyInjection.IServiceScope;
using ServiceProviderServiceExtensions = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions;

namespace Conflux.API.Tests.Controllers;

public class SURFSharekitControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions;
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    private const string WebhookCreatePayload =
        """
        {
          "attributes": {
            "owner": {
              "id": "eb98be07-f863-4815-805a-ad7f6cdea765",
              "name": "Zooma University",
              "type": "organisation"
            },
            "mboDomain": [],
            "mboDiscipline": [],
            "typicalAgeRange": {
              "string": null
            },
            "cost": {
              "source": null,
              "value": null
            },
            "urn:nbn": null,
            "modifiedAt": "2025-03-21T13:14:32Z",
            "title": "Webhook test - titel",
            "subtitle": "Webhook test - ondertitel",
            "publishers": [
              "Zooma University"
            ],
            "publishedAt": "2025",
            "place": null,
            "abstract": "Webhook test - samenvatting",
            "keywords": [
              "Webhook test"
            ],
            "numOfPages": null,
            "links": [
              {
                "url": "https://acc.surfsharekit.nl/link/c8a71cc4-8e0a-45ec-84fd-b27511bca116",
                "accessRight": "openaccess",
                "urlName": "Zooma",
                "important": "1"
              }
            ],
            "authors": [
              {
                "person": {
                  "id": "8b48a18f-1726-4269-9a7b-4279c967f35b",
                  "name": "Lucas Slim",
                  "email": "lslim@live.nl",
                  "dai": null,
                  "orcid": null,
                  "isni": null
                },
                "role": null,
                "external": null,
                "alias": null
              }
            ],
            "files": [
              {
                "fileName": "DummyFile",
                "accessRight": "openaccess",
                "url": "https://acc.surfsharekit.nl/objectstore/1a9bd201-897d-4852-8bb2-59c9364d0d3d",
                "resourceMimeType": "application/pdf",
                "usageRight": "pdm-10",
                "important": "1",
                "eTag": null
              }
            ],
            "institutes": null,
            "language": "nl",
            "themesResearchObject": null,
            "termsOfUse": null,
            "educationalLevels": [
              {
                "source": "http://purl.edustandaard.nl/vdex_context_czp_20060628.xml",
                "value": "HBO"
              }
            ],
            "typeResearchObject": null,
            "typesLearningMaterial": [],
            "themesLearningMaterial": [
              "onderwijs_opvoeding"
            ],
            "hasParts": [],
            "partOf": [],
            "technicalFormat": null,
            "vocabularies": {
              "vocabularyZiezo": [],
              "vocabularyDas": [],
              "vocabularyInformationLiteracy": [],
              "vocabularyVerpleegkunde": [],
              "vocabularyVaktherapie": []
            },
            "aggregationlevel": "3",
            "intendedUser": "author",
            "raid": null,
            "siaFileNum": null,
            "doi": null,
            "handle": null,
            "availability": null,
            "publishedIn": {
              "title": null,
              "publisherDocument": null,
              "placeOfPublication": null,
              "year": null,
              "issue": null,
              "edition": null,
              "issn": null,
              "isbn": null,
              "pageStart": null,
              "pageEnd": null
            },
            "conference": null
          },
          "type": "repoItem",
          "id": "webhookcreatepayload-id"
        }
        """;

    private const string WebhookDeletePayload =
        """
        {
          "attributes": [],
          "type": "repoItem",
          "meta": {
            "status": "deleted",
            "deletedAt": "2025-03-21T15:00:56Z"
          },
          "id": "69ed4ccb-1825-48d4-8c6f-53f8e4b78f88"
        }
        """;
    // Not available yet
    // public const string WebhookUpdatePayload =
    //     """
    //     Not available yet
    //     """;

    static SURFSharekitControllerTests()
    {
        JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };
        // allow "Primary", "Secondary", etc. to bind into TitleType/DescriptionType enum properties
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public SURFSharekitControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostWebhook_ReturnsSuccess()
    {
        HttpResponseMessage response =
            await _client.PostAsync("/sharekit/webhook/", new StringContent(WebhookCreatePayload));
        // response.EnsureSuccessStatusCode();

        var projects = await response.Content.ReadAsStringAsync();
        Assert.Equal("", projects);
        // Assert.NotNull(projects);
    }
}
