// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Session;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Conflux.API.Tests;

public class WebApplicationFactoryTests : WebApplicationFactory<Program>
{
    // This is a unique name for the in-memory database to avoid conflicts between tests
    private readonly string _databaseName = $"InMemoryConfluxTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the existing ConfluxContext registration
            ServiceDescriptor? descriptor =
                services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ConfluxContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add in-memory DB context
            services.AddDbContext<ConfluxContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            // Build and seed
            ServiceProvider provider = services.BuildServiceProvider();

            using IServiceScope scope = provider.CreateScope();
            ConfluxContext db = scope.ServiceProvider.GetRequiredService<ConfluxContext>();
            db.Database.EnsureCreated();
            // Clear the database
            db.Database.EnsureDeleted();

            // Check if test data is already seeded
            if (db.Projects.Any())
                return;

            // Add some projects
            db.Projects.Add(new()
            {
                Id = new("00000000-0000-0000-0000-000000000001"),
                Titles =
                [
                    new()
                    {
                        ProjectId = new("00000000-0000-0000-0000-000000000001"),
                        Id = Guid.NewGuid(),
                        Text = "Test Project",
                        Type = TitleType.Primary,
                        StartDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    },
                ],
                Descriptions =
                [
                    new()
                    {
                        ProjectId = new("00000000-0000-0000-0000-000000000001"),
                        Text = "This is a test project.",
                        Type = DescriptionType.Primary,
                        Language = Language.ENGLISH,
                    },
                ],
                StartDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                SCIMId = "SCIM",
            });

            // Add projects for the PUT and PATCH tests
            db.Projects.Add(new()
            {
                Id = new("00000000-0000-0000-0000-000000000002"),
                Titles =
                [
                    new()
                    {
                        ProjectId = new("00000000-0000-0000-0000-000000000002"),
                        Id = Guid.NewGuid(),
                        Text = "Test Project 2",
                        Type = TitleType.Primary,
                        StartDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    },
                ],
                Descriptions =
                [
                    new()
                    {
                        ProjectId = new("00000000-0000-0000-0000-000000000002"),
                        Text = "This is a test project.",
                        Type = DescriptionType.Primary,
                        Language = Language.ENGLISH,
                    },
                ],
                StartDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                SCIMId = "SCIM",
            });
            db.Projects.Add(new()
            {
                Id = new("00000000-0000-0000-0000-000000000003"),
                Titles =
                [
                    new()
                    {
                        ProjectId = new("00000000-0000-0000-0000-000000000003"),
                        Id = Guid.NewGuid(),
                        Text = "Test Project 3",
                        Type = TitleType.Primary,
                        StartDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    },
                ],
                Descriptions =
                [
                    new()
                    {
                        ProjectId = new("00000000-0000-0000-0000-000000000003"),
                        Text = "This is a test project.",
                        Type = DescriptionType.Primary,
                        Language = Language.ENGLISH,
                    },
                ],
                StartDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                SCIMId = "SCIM",
            });

            db.People.Add(new()
            {
                Id = new("00000000-0000-0000-0000-000000000001"),
                Name = "John Doe",
                GivenName = "John",
                FamilyName = "Doe",
                Email = "john@doe.nl",
            });

            // Add a test user with admin roles for all test projects
            Guid testUserId = new("00000000-0000-0000-0000-000000000001");
            User testUser = new()
            {
                Id = testUserId,
                Name = "Test Admin",
                SCIMId = "test-admin-scim-id",
            };
            db.Users.Add(testUser);

            // Add roles for the test user
            db.UserRoles.Add(new()
            {
                Id = Guid.NewGuid(),
                ProjectId = new("00000000-0000-0000-0000-000000000001"),
                Type = UserRoleType.Admin,
                Urn = "test:urn:1",
                SCIMId = "test-admin-scim-id",
            });

            db.UserRoles.Add(new()
            {
                Id = Guid.NewGuid(),
                ProjectId = new("00000000-0000-0000-0000-000000000002"),
                Type = UserRoleType.Admin,
                Urn = "test:urn:2",
                SCIMId = "test-admin-scim-id",
            });

            db.UserRoles.Add(new()
            {
                Id = Guid.NewGuid(),
                ProjectId = new("00000000-0000-0000-0000-000000000003"),
                Type = UserRoleType.Admin,
                Urn = "test:urn:3",
                SCIMId = "test-admin-scim-id",
            });

            db.SaveChanges();

            // Mock the user session service to return our test admin user
            Mock<IUserSessionService> mockUserSessionService = new();
            mockUserSessionService.Setup(m => m.GetUser()).ReturnsAsync(new UserSession
            {
                User = testUser,
                Collaborations =
                [
                    new()
                    {
                        Organization = "Test Organization",
                        CollaborationGroup = new()
                        {
                            Id = "group1",
                            Urn = "test:urn:1",
                            DisplayName = "Test Group 1",
                            ExternalId = "ext1",
                            SCIMId = "SCIM", // This should match the projects' SCIMId
                        },
                        Groups =
                        [
                            new()
                            {
                                Id = "group1",
                                Urn = "test:urn:1",
                                DisplayName = "Test Group 1",
                                ExternalId = "ext1",
                                SCIMId = "SCIM",
                            },
                        ],
                    },
                ],
            });

            // Replace the actual service with our mock
            services.AddScoped<IUserSessionService>(_ => mockUserSessionService.Object);

            // Mock the access control service to allow our test admin user to access all projects with admin role
            Mock<IAccessControlService> mockAccessControlService = new();
            mockAccessControlService.Setup(m => m.UserHasRoleInProject(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<UserRoleType>())).ReturnsAsync(true);

            // Replace the actual service with our mock
            services.AddScoped<IAccessControlService>(sp => mockAccessControlService.Object);
        });
    }
}
