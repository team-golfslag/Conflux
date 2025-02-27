using Conflux.Core;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContextPool<ConfluxContext>(opt =>
                                                      opt.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

WebApplication app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Ensure the database is created and seeded
using (IServiceScope scope = app.Services.CreateScope())
{
    IServiceProvider services = scope.ServiceProvider;
    ConfluxContext context = services.GetRequiredService<ConfluxContext>();
    await context.Database.MigrateAsync();
}

app.MapSwagger();

await app.RunAsync();