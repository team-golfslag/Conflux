using Microsoft.EntityFrameworkCore;

namespace Conflux.Core;

public class ConfluxContext(DbContextOptions<ConfluxContext> options) : DbContext(options)
{
    public DbSet<Person> People { get; set; }
}
