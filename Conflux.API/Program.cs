// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Text.Json;
using Conflux.Data;
using Conflux.Domain.Logic.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Conflux.API;

#pragma warning disable S1118 // Since we run integration tests in Conflux.API.Tests, we need a public Program class
public class Program
#pragma warning restore S1118
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            options.JsonSerializerOptions.WriteIndented = true;
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddControllers();
        builder.Services.AddSwaggerGen();
        if (builder.Environment.EnvironmentName != "Testing")
        {
            string? connectionString = builder.Configuration.GetConnectionString("Database") ??
                GetConnectionStringFromEnvironment();
            builder.Services.AddDbContextPool<ConfluxContext>(opt =>
                opt.UseNpgsql(
                    connectionString,
                    npgsqlOptions =>
                        npgsqlOptions.MigrationsAssembly("Conflux.Data")));
        }
            
        

        string[]? allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins is null || allowedOrigins.Length == 0)
            throw new InvalidOperationException("Allowed origins must be specified in configuration.");

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowLocalhost", policy =>
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

        // Add exception handling middleware
        app.UseExceptionHandler(appBuilder =>
        {
            appBuilder.Run(async context =>
            {
                IExceptionHandlerFeature? exception = context.Features.Get<IExceptionHandlerFeature>();
                switch (exception?.Error)
                {
                    case ProjectNotFoundException:
                    case PersonNotFoundException:
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = exception.Error.Message,
                        });
                        break;
                    case PersonAlreadyAddedToProjectException:
                        context.Response.StatusCode = 409; // Conflict
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = exception.Error.Message,
                        });
                        break;
                    default:
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = "An unexpected error occurred.",
                        });
                        break;
                }
            });
        });

        app.UseHttpsRedirection();
        app.MapControllers();
        app.UseCors("AllowLocalhost");

        // Ensure the database is created and seeded
        using IServiceScope scope = app.Services.CreateScope();
        IServiceProvider services = scope.ServiceProvider;
        ConfluxContext context = services.GetRequiredService<ConfluxContext>();
        if (context.Database.IsRelational()) await context.Database.MigrateAsync();

        // Seed the database for development, if necessary
        if (app.Environment.IsDevelopment() && !await context.People.AnyAsync())
            await context.SeedDataAsync();

        app.MapSwagger();

        await app.RunAsync();
    }
    
    /// <summary>
    /// Gets the connection string from environment variables.
    /// </summary>
    /// <returns>The connection string.</returns>
    public static string GetConnectionStringFromEnvironment()
    {
        string? host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
        string? port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        string? database = Environment.GetEnvironmentVariable("DB_NAME");
        if (string.IsNullOrEmpty(database))
            throw new InvalidOperationException("Database name must be specified in environment variables.");
        string? user = Environment.GetEnvironmentVariable("DB_USER");
        if (string.IsNullOrEmpty(user))
            throw new InvalidOperationException("Database user must be specified in environment variables.");
        string? password = Environment.GetEnvironmentVariable("DB_PASSWORD");
        if (string.IsNullOrEmpty(password))
            throw new InvalidOperationException("Database password must be specified in environment variables.");

        return $"Host={host}:{port};Database={database};Username={user};Password={password}";
    }
}
