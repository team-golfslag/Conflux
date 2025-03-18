using Conflux.Data;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContextPool<ConfluxContext>(opt => opt.UseNpgsql(
    builder.Configuration.GetConnectionString("Database"),
    npgsqlOptions =>
        npgsqlOptions.MigrationsAssembly("Conflux.Data")));

WebApplication app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

// Ensure the database is created and seeded
using IServiceScope scope = app.Services.CreateScope();
IServiceProvider services = scope.ServiceProvider;
ConfluxContext context = services.GetRequiredService<ConfluxContext>();
await context.Database.MigrateAsync();

// Seed the database for development, if necessary
if (app.Environment.IsDevelopment() && !await context.People.AnyAsync())
    await context.SeedDataAsync();

app.MapSwagger();

await app.RunAsync();
