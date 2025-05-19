// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Integrations.SURFSharekit.Tests;

public class SURFSharekitJsonReponse
{
    public const string DummyResponse =
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

    public const string WebhookCreatePayload =
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

    public const string WebhookDeletePayload =
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

}
