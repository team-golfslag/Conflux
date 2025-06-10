// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Security.Cryptography;
using RAiD.Net.Domain;

namespace Conflux.Integrations.RAiD;

public static class Extensions
{
    public static string GetHash(this RAiDUpdateRequest updateRequest)
    {
        // force null because this identifier can change with version
        RAiDUpdateRequest req = new RAiDUpdateRequest
        {
            Metadata = updateRequest.Metadata,
            Identifier = null!,
            Title = updateRequest.Title!,
            Date = updateRequest.Date,
            Description = updateRequest.Description,
            Access = updateRequest.Access,
            AlternateUrl = updateRequest.AlternateUrl,
            Contributor = updateRequest.Contributor,
            Organisation = updateRequest.Organisation,
            Subject = updateRequest.Subject,
            RelatedRaid = updateRequest.RelatedRaid,
            RelatedObject = updateRequest.RelatedObject,
            AlternateIdentifier = updateRequest.AlternateIdentifier,
            SpatialCoverage = updateRequest.SpatialCoverage,
        };
        string json = System.Text.Json.JsonSerializer.Serialize(req);
        byte[] hashBytes = MD5.HashData(System.Text.Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
