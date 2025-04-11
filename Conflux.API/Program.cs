// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Text.Json;
using Conflux.Data;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Conflux.RepositoryConnections.SRAM;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

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
        builder.Services.AddSwaggerDocument(c => { c.Title = "Conflux API"; });
        if (builder.Environment.EnvironmentName != "Testing")
        {
            string? connectionString = builder.Configuration.GetConnectionString("Database") ??
                ConnectionStringHelper.GetConnectionStringFromEnvironment();
            builder.Services.AddDbContextPool<ConfluxContext>(opt =>
                opt.UseNpgsql(
                    connectionString,
                    npgsqlOptions =>
                        npgsqlOptions.MigrationsAssembly("Conflux.Data")));
        }

        builder.Services.AddDistributedMemoryCache();

        builder.Services.AddSession(options =>
        {
            // Set session idle timeout to 20 minutes for production environments.
            options.IdleTimeout = TimeSpan.FromMinutes(20);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHttpClient<SCIMApiClient>(client =>
        {
            client.BaseAddress = new("https://sram.surf.nl/api/scim/v2/");
        });
        builder.Services.AddScoped<SCIMApiClient>(provider =>
        {
            IHttpClientFactory httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            HttpClient client = httpClientFactory.CreateClient(nameof(SCIMApiClient));

            SCIMApiClient scimClient = new(client);

            string? secret = Environment.GetEnvironmentVariable("SRAM_SCIM_SECRET");
            if (string.IsNullOrEmpty(secret))
                throw new InvalidOperationException("SRAM_SCIM_SECRET not set.");

            scimClient.SetBearerToken(secret);
            return scimClient;
        });
        builder.Services.AddScoped<CollaborationMapper>();
        builder.Services.AddScoped<IUserSessionService, UserSessionService>();

        // get sram secret from environment variable
        string? sramSecret = Environment.GetEnvironmentVariable("SRAM_CLIENT_SECRET");
        if (string.IsNullOrEmpty(sramSecret))
            throw new InvalidOperationException("SRAM secret must be specified in environment variable.");

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Events.OnSignedIn = context =>
                {
                    // Set user session
                    IUserSessionService userSessionService =
                        context.HttpContext.RequestServices.GetRequiredService<IUserSessionService>();
                    userSessionService.SetUser(context.Principal);
                    return Task.CompletedTask;
                };
                options.Events.OnSigningOut = context =>
                {
                    // Clear user session
                    IUserSessionService userSessionService =
                        context.HttpContext.RequestServices.GetRequiredService<IUserSessionService>();
                    userSessionService.ClearUser();
                    return Task.CompletedTask;
                };
            })
            .AddOpenIdConnect(options =>
            {
                IConfigurationSection oidcConfig = builder.Configuration.GetSection("Authentication:SRAM");

                options.Authority = oidcConfig["Authority"];
                options.ClientId = oidcConfig["ClientId"];
                options.ClientSecret = sramSecret;

                options.CallbackPath = oidcConfig["CallbackPath"];
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;

                var scopes = oidcConfig.GetSection("Scopes").Get<List<string>>();
                if (scopes != null)
                    foreach (string scope in scopes)
                        options.Scope.Add(scope);

                var claimMappings = oidcConfig.GetSection("ClaimMappings").Get<Dictionary<string, string>>();
                if (claimMappings != null)
                    foreach (var mapping in claimMappings)
                        options.ClaimActions.MapJsonKey(mapping.Key, mapping.Value);

                options.Events.OnRedirectToIdentityProvider = context =>
                {
                    context.ProtocolMessage.RedirectUri = oidcConfig["RedirectUri"];
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToIdentityProviderForSignOut = context =>
                {
                    context.Response.Redirect(oidcConfig["SignOutCallbackPath"]!);
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
            });

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
            app.UseOpenApi();
            app.UseSwaggerUi(c => { c.DocumentTitle = "Conflux API"; });
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
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseSession();

        app.UseCors("AllowLocalhost");

        // Ensure the database is created and seeded
        using IServiceScope scope = app.Services.CreateScope();
        IServiceProvider services = scope.ServiceProvider;
        ConfluxContext context = services.GetRequiredService<ConfluxContext>();
        if (context.Database.IsRelational()) await context.Database.MigrateAsync();

        // Seed the database for development, if necessary
        if (app.Environment.IsDevelopment() && !await context.People.AnyAsync())
            await context.SeedDataAsync();

        await app.RunAsync();
    }
}
