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

string[]? allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (allowedOrigins is null)
    throw new InvalidOperationException("Allowed origins must be specified in configuration.");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});


WebApplication app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.UseCors("AllowAll");

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
