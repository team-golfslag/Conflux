// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using RAiD.Net.Domain;

namespace Conflux.Integrations.RAiD;

public static class ProjectMapper
{
    public static RAiDCreateRequest MapProjectCreation(Project project) =>
        new()
        {
            Title = project.Titles.ConvertAll(MapProjectTitle),
            Date = new()
            {
                StartDate = project.StartDate,
                EndDate = project.EndDate,
            },

            Description =

            [
                new()
                {
                    Text = project.Description ?? string.Empty,
                    Type = new() // TODO make an enum for this and set SchemaUri default value
                    {
                        Id = "https://vocabulary.raid.org/description.type.id/326",
                        SchemaUri = "https://vocabulary.raid.org/description.type.schema/320",
                    },
                    Language = new() // TODO make an enum for this and set SchemaUri default value
                    {
                        Id = "eng",
                        SchemaUri = "https://www.iso.org/standard/74575.html",
                    },
                },
            ],
            Access = new()
            {
                Type = new() // TODO make an enum for this
                {
                    Id = "https://vocabularies.coar-repositories.org/access_rights/c_abf2/",
                    SchemaUri = "https://vocabularies.coar-repositories.org/access_rights/",
                },
                EmbargoExpiry = null, // Not implemented for now
                Statement = null,     // Not implemented for now
            },
            Contributor = project.Contributors.ConvertAll(MapContributor),
            Organisation = project.Organisations.ConvertAll(MapOrganisation),
            Subject = null,     // Not implemented for now
            RelatedRaid = null, // Not implemented for now
            RelatedObject = project.Products.ConvertAll(MapProduct),
            AlternateIdentifier = null, // Not implemented for now
            SpatialCoverage = null,     // Not implemented for now
        };

    private static RAiDTitle MapProjectTitle(ProjectTitle title) =>
        new()
        {
            Text = title.Text,
            Type = new()
            {
                Id = title.TypeUri,
                SchemaUri = title.TypeSchemaUri,
            },
            StartDate = title.StartDate,
            EndDate = title.EndDate,
            Language = title.Language == null
                ? null
                : new()
                {
                    SchemaUri = title.LanguageSchemaUri,
                    Id = title.Language,
                },
        };

    private static RAiDContributorRole MapContributorRole(ContributorRole role) =>
        new()
        {
            SchemaUri = role.SchemaUri,
            Id = role.GetUri(),
        };

    private static RAiDContributorPosition MapContributorPosition(ContributorPosition position) =>
        new()
        {
            SchemaUri = position.SchemaUri,
            Id = position.GetUri,
            StartDate = position.StartDate,
            EndDate = position.EndDate,
        };

    private static RAiDContributor MapContributor(Contributor contributor) =>
        new()
        {
            SchemaUri = contributor.SchemaUri,
            Email = contributor.Email,
            Uuid = null,
            Position = contributor.Positions.Select(MapContributorPosition).ToList(),
            Role = contributor.Roles.Select(MapContributorRole).ToList(),
            Leader = false,
            Contact = false,
        };

    private static RAiDOrganisationRole MapOrganisationRole(OrganisationRole role) =>
        new()
        {
            SchemaUri = role.SchemaUri,
            Id = role.GetUri,
            StartDate = role.StartDate,
            EndDate = role.EndDate,
        };

    private static RAiDOrganisation MapOrganisation(Organisation organisation) =>
        new()
        {
            Id = organisation.RORId ?? throw new ArgumentNullException(nameof(organisation)),
            SchemaUri = organisation.SchemaUri,
            Role = organisation.Roles.Select(MapOrganisationRole).ToList(),
        };

    private static RAiDRelatedObject MapProduct(Product product) => throw new NotImplementedException();
}
