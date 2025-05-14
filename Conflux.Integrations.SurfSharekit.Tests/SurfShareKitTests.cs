// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Net;
using Conflux;
using Conflux.Domain;
using Conflux.Integrations.SURFSharekit;
using Conflux.Integrations.SURFSharekit.Tests.Helpers;

using SURFSharekit.Net;
using SURFSharekit.Net.Models;
using Xunit;
using Xunit.Abstractions;

namespace Conflux.Integrations.SurfSharekit.Tests;

public class SurfShareKitTests
{
    
    private readonly ITestOutputHelper _output;

    public SurfShareKitTests(ITestOutputHelper output)
    {
        this._output = output;
    }
    
    /// <summary>
    /// This is a test to see if the ProductMaper.MultipleRepoItemsToProducts given a list of RepoItems
    /// returns a valid list of Products
    /// </summary>
    [Fact]
    public async Task SurfShareKit_GetMultipleToProductList_Test()
    {
        const string json = """
                           {
                              "meta": {
                                  "totalCount": 2
                              },
                              "filters": [],
                              "links": {
                                  "first": "/api/jsonapi/channel/v1/demo_projectenmodule/repoItems?page[size]=10&page[number]=1",
                                  "self": "/api/jsonapi/channel/v1/demo_projectenmodule/repoItems?page[size]=10&page[number]=1",
                                  "last": "/api/jsonapi/channel/v1/demo_projectenmodule/repoItems?page[size]=10&page[number]=1"
                              },
                              "data": [
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
                                  },
                                  {"attributes": {} },
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
                                         "title": "",
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
                              ]
                          }
                          """;
        
        FakeHttpMessageHandler handler = new(json, HttpStatusCode.OK);
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new("https://dummy/"),
        };
        SURFSharekitApiClient client = new(httpClient);

        List<SURFSharekitRepoItem>? result = await client.GetAllRepoItems();

        List<Product> allProducts = ProductMapper.MultipleRepoItemsToProducts(result!);
        
        Assert.Single(allProducts);
        
        Product product = allProducts[0];
        
        Assert.NotNull(product);
        Assert.NotNull(product.Title);
        Assert.Equal("Test 1 voor PoC Projectenmodule", product.Title);
        
        Assert.NotNull(product.Url);
        Assert.Equal("https://doi.org/10.1000/182", product.Url);
        
        Assert.IsType<Guid>(product.Id);
        Assert.Equal(product.Schema, ProductSchema.Doi);
    }
    
    
    /// <summary>
    /// When we receive a singular repoItem by getting it per id,
    /// ProductMapper.SingleRepoItemToProduct should return a valid Product
    /// </summary>
    [Fact]
    public async Task SurfShareKit_GetPerIdToProduct_Test()
    {
        const string json = """
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

        FakeHttpMessageHandler handler = new(json, HttpStatusCode.OK);
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new("https://dummy/"),
        };
        SURFSharekitApiClient client = new(httpClient);
        SURFSharekitRepoItem? item = await client.GetRepoItemById("dummy-id");
        Product? product = ProductMapper.SingleRepoItemToProduct(item!);
        
        Assert.NotNull(product);
        Assert.NotNull(product.Title);
        Assert.Equal("Test 1 voor PoC Projectenmodule", product.Title);
        
        Assert.NotNull(product.Url);
        Assert.Equal("https://doi.org/10.1000/182", product.Url);
        
        Assert.IsType<Guid>(product.Id);
        Assert.Equal(product.Schema, ProductSchema.Doi);
        
    }
}
