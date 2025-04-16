// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Conflux.API.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    // This is a unique name for the in-memory database to avoid conflicts between tests
    private readonly string _databaseName = $"InMemoryConfluxTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the existing ConfluxContext registration
            ServiceDescriptor? descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ConfluxContext>));
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
                Title = "Test Project",
                Description = "This is a test project.",
                StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                SCIMId = "SRAM",
            });

            // Add projects for the PUT and PATCH tests
            db.Projects.Add(new()
            {
                Id = new("00000000-0000-0000-0000-000000000002"),
                Title = "Test Project 2",
                Description = "This is a test project.",
                StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                SCIMId = "SRAM",
            });
            db.Projects.Add(new()
            {
                Id = new("00000000-0000-0000-0000-000000000003"),
                Title = "Test Project 3",
                Description = "This is a test project.",
                StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                SCIMId = "SRAM",
            });

            db.SaveChanges();
        });
    }
}
