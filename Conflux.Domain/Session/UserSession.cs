// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Session;

public class UserSession
{
    public static readonly Guid DevelopmentUserId = Guid.Parse("b0ee16ff-6e23-4266-b503-b93a003c1c05");
    public string SRAMId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GivenName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public User? User { get; set; }
    public List<Collaboration> Collaborations { get; set; } = [];

    public static UserSession Development()
    {
        string sramId = DevelopmentUserId + "@sram.surf.nl";
        return new()
        {
            SRAMId = sramId,
            Name = "Development User",
            GivenName = "Development",
            FamilyName = "User",
            Email = "development@sram.surf.nl",
            User = new()
            {
                Id = DevelopmentUserId,
                SRAMId = sramId,
                SCIMId = DevelopmentUserId + "@scim.sram.surf.nl",
                ORCiD = null,
                Name = "Development User",
                Roles = [],
                GivenName = "Development",
                FamilyName = "User",
                Email = "development@sram.surf.nl",
            },
            Collaborations =
            [
                new()
                {
                    CollaborationGroup = new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Urn = "urn:mace:surf.nl:sram:group:surf:development",
                        DisplayName = "Development Group",
                        Description = "This is a development group.",
#pragma warning disable S1075 // Refactor your code not tu use hardcoded URIs
                        Url = "https://example.com/development",
                        LogoUrl = "https://example.com/logo.png",
#pragma warning restore S1075
                        ExternalId = Guid.NewGuid().ToString(),
                        SCIMId = "SCIM",
                    },
                    Organization = "Development Organization",
                    Groups =
                    [
                        new()
                        {
                            Id = Guid.NewGuid().ToString(),
                            Urn = "urn:mace:surf.nl:sram:group:surf:development:conflux-cx_project_admin",
                            DisplayName = "Development Group",
                            Description = "This is a development group.",
#pragma warning disable S1075 // Refactor your code not tu use hardcoded URIs
                            Url = "https://example.com/development",
                            LogoUrl = "https://example.com/logo.png",
#pragma warning restore S1075
                            ExternalId = Guid.NewGuid().ToString(),
                            SCIMId = Guid.NewGuid().ToString(),
                        },
                    ],
                },
            ],
        };
    }
}
