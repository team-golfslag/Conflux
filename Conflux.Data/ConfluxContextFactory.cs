using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Conflux.Data;

public class ConfluxContextFactory : IDesignTimeDbContextFactory<ConfluxContext>
{
    public ConfluxContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<ConfluxContext> optionsBuilder = new();
        
        optionsBuilder.UseNpgsql("Host=localhost:5432;Database=conflux-dev;Username=conflux-dev;Password=conflux-dev"); 
        
        optionsBuilder.UseNpgsql();

        return new(optionsBuilder.Options);
    }
}
