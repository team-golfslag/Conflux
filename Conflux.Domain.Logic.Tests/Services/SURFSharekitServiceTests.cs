// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.Services;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using SURFSharekit.Net.Models.RepoItem;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class SURFSharekitServiceTests
{
    Mock<ISURFSharekitService> _surfSharekitService;

    public SURFSharekitServiceTests()
    {
        _surfSharekitService = new Mock<ISURFSharekitService>();
    }

    private const string OriginalRepoItemJson =
        """
        {
            "attributes": {
                "owner": {
                    "id": "51249c5c-00ed-4cb6-858e-13ef6eb56e48",
                    "name": "Demo institute",
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
                "modifiedAt": "2025-03-18T12:59:31Z",
                "title": "Test 1 voor PoC Projectenmodule",
                "subtitle": null,
                "publishers": [
                    "Demo institute"
                ],
                "publishedAt": "2025",
                "place": "Utrecht",
                "abstract": "Viooltjes houden van een warme zonnige plek, maar kunnen ook prima bloeien in de halfschaduw. Ze houden van veel water en het is belangrijk dat de grond licht vochtig is. We raden je aan om uitgebloeide bloemetjes eruit te knijpen. Hierdoor worden de nieuwe bloempjes gestimuleerd om te groeien",
                "keywords": [
                    "viooltjes",
                    "bloemen",
                    "paars",
                    "natuur"
                ],
                "numOfPages": null,
                "links": [
                    {
                        "url": "https://acc.surfsharekit.nl/link/16ec39fb-01e6-4fff-b2cf-b02893c39609",
                        "accessRight": "openaccess",
                        "urlName": "Publinova",
                        "important": null
                    }
                ],
                "authors": [
                    {
                        "person": {
                            "id": "ee5bf18e-3b0b-4dc3-9617-f067096562a8",
                            "name": "test auteur",
                            "email": "testauteur@123.com",
                            "dai": null,
                            "orcid": null,
                            "isni": null
                        },
                        "role": "Docent",
                        "external": null,
                        "alias": null
                    },
                    {
                        "person": {
                            "id": "a5ff7e34-ec5e-4f92-9556-e757d0631776",
                            "name": "Test Auteur 2",
                            "email": null,
                            "dai": null,
                            "orcid": null,
                            "isni": null
                        },
                        "role": "Lector",
                        "external": "1",
                        "alias": null
                    }
                ],
                "files": [
                    {
                        "fileName": "Screenshot 2023-05-24 092515",
                        "accessRight": "openaccess",
                        "url": "https://acc.surfsharekit.nl/objectstore/ae688204-99b4-46c7-9426-2e12cc35833c",
                        "resourceMimeType": "image/png",
                        "usageRight": "cc-by-40",
                        "important": null,
                        "eTag": null
                    }
                ],
                "institutes": [
                    {
                        "name": "Afdeling B",
                        "type": "department",
                        "id": "6f984918-7867-4275-94b2-ceba458ee869"
                    }
                ],
                "language": "nl",
                "themesResearchObject": "natuur_landbouw",
                "termsOfUse": null,
                "educationalLevels": null,
                "typeResearchObject": "Artikel",
                "typesLearningMaterial": [],
                "themesLearningMaterial": [],
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
                "aggregationlevel": null,
                "intendedUser": null,
                "raid": "10.26259/089177da",
                "siaFileNum": null,
                "doi": "https://doi.org/10.1000/182",
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
            "id": "dummy-id"
        }
        """;
    private static SURFSharekitRepoItem GetTestItem() => 
        JsonConvert.DeserializeObject<SURFSharekitRepoItem>(OriginalRepoItemJson)!;

    [Fact]
    public void ProcessRepoItem_ShouldThrow_WhenPayloadFormatIsInvalid()
    {
        // TODO: Because WebhookCreate is the same as a repo item, this cannot happen (yet) 
        
        // Arrange
        // Act
        // Assert
    }

    [Fact]
    public async Task ProcessRepoItem_ShouldReturnNull_WhenRaidIsNull()
    {
        // Arrange
        SURFSharekitRepoItem testItem = GetTestItem();
        testItem.Attributes!.Raid = null;
        
        // Act
        var processedRepoItem = await _surfSharekitService.Object.ProcessRepoItem(testItem);
        
        // Assert
        Assert.Null(processedRepoItem);
    }

    [Fact]
    public async Task ProcessRepoItem_ShouldReturnNull_WhenIdIsNull()
    {
        // Arrange
        SURFSharekitRepoItem testItem = GetTestItem();
        testItem.Id = null;
        
        // Act
        var processedRepoItem = await _surfSharekitService.Object.ProcessRepoItem(testItem);
        
        // Assert
        Assert.Null(processedRepoItem);
    }

    [Fact]
    public async Task ProcessRepoItem_ShouldReturnNull_WhenAttributesIsNull()
    {
        // Arrange
        SURFSharekitRepoItem testItem = GetTestItem();
        testItem.Attributes = null;
        
        // Act
        var processedRepoItem = await _surfSharekitService.Object.ProcessRepoItem(testItem);
        
        // Assert
        Assert.Null(processedRepoItem);
    }

    [Fact]
    public async Task ProcessRepoItem_ShouldReturn_WhenAttributesIsEmpty()
    {
        // Arrange
        SURFSharekitRepoItem testItem = GetTestItem();
        testItem.Attributes
        // Act
        
        // Assert
    }
}
