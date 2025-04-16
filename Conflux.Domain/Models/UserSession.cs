// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Models;

public class UserSession
{
    public string SRAMId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GivenName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public User? User { get; set; }
    public List<Collaboration> Collaborations { get; set; } = [];

    public static UserSession Development()
    {
        string sramId = Guid.NewGuid() + "@sram.surf.nl";
        return new()
        {
            SRAMId = sramId,
            Name = "Development User",
            GivenName = "Development",
            FamilyName = "User",
            Email = "development@sram.surf.nl",
            User = new User
            {
                Id = Guid.NewGuid(),
                SRAMId = sramId,
                SCIMId =  Guid.NewGuid() + "@scim.sram.surf.nl",
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
                        Url = "https://example.com/development",
                        LogoUrl = "https://example.com/logo.png",
                        ExternalId = Guid.NewGuid().ToString(),
                        SCIMId = "SRAM",
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
                            Url = "https://example.com/development",
                            LogoUrl = "https://example.com/logo.png",
                            ExternalId = Guid.NewGuid().ToString(),
                            SCIMId = Guid.NewGuid().ToString(),
                        },
                    ],
                },
            ],
        };
    }
}
