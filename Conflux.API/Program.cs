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
            builder.Services.AddDbContextPool<ConfluxContext>(opt =>
                opt.UseNpgsql(
                    builder.Configuration.GetConnectionString("Database"),
                    npgsqlOptions =>
                        npgsqlOptions.MigrationsAssembly("Conflux.Data")));

        string[]? allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins is null  || allowedOrigins.Length == 0)
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
                if (exception?.Error is ProjectNotFoundException)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = exception.Error.Message,
                    });
                }
                else
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "An unexpected error occurred.",
                    });
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
}
