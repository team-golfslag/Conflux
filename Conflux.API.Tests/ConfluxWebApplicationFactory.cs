using Conflux.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Conflux.API.Tests;

public class ConfluxWebApplicationFactory : WebApplicationFactory<Program>
{
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
                options.UseInMemoryDatabase("InMemoryConfluxTestDb"));

            // Build and seed
            ServiceProvider provider = services.BuildServiceProvider();

            using IServiceScope scope = provider.CreateScope();
            ConfluxContext db = scope.ServiceProvider.GetRequiredService<ConfluxContext>();
            db.Database.EnsureCreated();
        });
    }
}
