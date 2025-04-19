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
using Microsoft.Extensions.Primitives;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Conflux.API;

#pragma warning disable S1118
public class Program
#pragma warning restore S1118
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Configure services
        builder.Services.AddFeatureManagement(builder.Configuration.GetSection("FeatureFlags"));
#pragma warning disable ASP0000
        IVariantFeatureManager featureManager =
            builder.Services.BuildServiceProvider().GetRequiredService<IVariantFeatureManager>();
#pragma warning restore ASP0000

        await ConfigureServices(builder, featureManager);
        await ConfigureAuthentication(builder, featureManager);
        ConfigureCors(builder);

        WebApplication app = builder.Build();

        // Configure middleware
        await ConfigureMiddleware(app, featureManager);

        // Initialize database
        await InitializeDatabase(app, featureManager);

        await app.RunAsync();
    }

    private static async Task ConfigureServices(WebApplicationBuilder builder, IVariantFeatureManager featureManager)
    {
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            options.JsonSerializerOptions.WriteIndented = true;
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerDocument(c => { c.Title = "Conflux API"; });

        await ConfigureDatabase(builder, featureManager);

        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(20);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        builder.Services.AddHttpContextAccessor();
        await ConfigureSRAMServices(builder, featureManager);
    }

    private static async Task ConfigureDatabase(WebApplicationBuilder builder, IVariantFeatureManager featureManager)
    {
        if (!await featureManager.IsEnabledAsync("DatabaseConnection"))
            return;

        string? connectionString = builder.Configuration.GetConnectionString("Database") ??
            ConnectionStringHelper.GetConnectionStringFromEnvironment();
        builder.Services.AddDbContextPool<ConfluxContext>(opt =>
            opt.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly("Conflux.Data")));
    }

    private static async Task ConfigureSRAMServices(WebApplicationBuilder builder,
        IVariantFeatureManager featureManager)
    {
        builder.Services.AddHttpClient<SCIMApiClient>(client =>
        {
            client.BaseAddress = new("https://sram.surf.nl/api/scim/v2/");
        });

        bool sramEnabled = await featureManager.IsEnabledAsync("SRAMAuthentication");
        builder.Services.AddSingleton<ISCIMApiClient, SCIMApiClient>(provider =>
        {
            HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(SCIMApiClient));
            SCIMApiClient scimClient = new(client);

            string? secret = Environment.GetEnvironmentVariable("SRAM_SCIM_SECRET");
            if (string.IsNullOrEmpty(secret) && sramEnabled)
                throw new InvalidOperationException("SRAM_SCIM_SECRET not set.");

            scimClient.SetBearerToken(secret!);
            return scimClient;
        });

        builder.Services.AddScoped<ICollaborationMapper, CollaborationMapper>();
        builder.Services.AddScoped<IUserSessionService, UserSessionService>();
        builder.Services.AddScoped<ISessionMappingService, SessionMappingService>();
        builder.Services.AddScoped<IProjectSyncService, ProjectSyncService>();
        builder.Services.AddScoped<ProjectsService>();
    }

    private static async Task ConfigureAuthentication(WebApplicationBuilder builder,
        IVariantFeatureManager featureManager)
    {
        bool sramEnabled = await featureManager.IsEnabledAsync("SRAMAuthentication");
        if (sramEnabled)
            SetupAuth(builder);
        else
            SetupDevelopmentAuth(builder);
    }

    private static void ConfigureCors(WebApplicationBuilder builder)
    {
        string[]? allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins is null || allowedOrigins.Length == 0)
            throw new InvalidOperationException("Allowed origins must be specified in configuration.");

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowLocalhost", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .AllowAnyHeader();
            });
        });
    }

    private static async Task ConfigureMiddleware(WebApplication app, IVariantFeatureManager featureManager)
    {
        if (await featureManager.IsEnabledAsync("Swagger"))
        {
            app.UseOpenApi();
            app.UseSwaggerUi(c => { c.DocumentTitle = "Conflux API"; });
        }

        app.UseExceptionHandler(appBuilder =>
        {
            appBuilder.Run(async context =>
            {
                Exception? exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                switch (exception)
                {
                    case ProjectNotFoundException or PersonNotFoundException:
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = exception.Message,
                        });
                        break;
                    case PersonAlreadyAddedToProjectException:
                        context.Response.StatusCode = 409;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = exception.Message,
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

        app.UseCors("AllowLocalhost");
        app.UseHttpsRedirection();
        app.MapControllers();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseSession();
    }

    private static async Task InitializeDatabase(WebApplication app, IVariantFeatureManager featureManager)
    {
        using IServiceScope scope = app.Services.CreateScope();
        IServiceProvider services = scope.ServiceProvider;

        if (services.GetService<ConfluxContext>() != null || await featureManager.IsEnabledAsync("DatabaseConnection"))
        {
            ConfluxContext context = services.GetRequiredService<ConfluxContext>();
            if (context.Database.IsRelational())
                await context.Database.MigrateAsync();

            if (await featureManager.IsEnabledAsync("SeedDatabase") && !await context.Users.AnyAsync())
                await context.SeedDataAsync();
        }
    }

    private static void SetupDevelopmentAuth(WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication("DevelopmentAuthScheme")
            .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthHandler>("DevelopmentAuthScheme", _ => { })
            .AddCookie(ConfigureCookieAuth);
    }

    private static void SetupAuth(WebApplicationBuilder builder)
    {
        string? sramSecret = Environment.GetEnvironmentVariable("SRAM_CLIENT_SECRET");
        if (string.IsNullOrEmpty(sramSecret))
            throw new InvalidOperationException("SRAM secret must be specified in environment variable.");

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(ConfigureCookieAuth)
            .AddOpenIdConnect(options => ConfigureOpenIdConnect(options, builder.Configuration, sramSecret));
    }

    private static void ConfigureCookieAuth(CookieAuthenticationOptions options)
    {
        options.Events.OnSignedIn = context =>
        {
            IUserSessionService userSessionService =
                context.HttpContext.RequestServices.GetRequiredService<IUserSessionService>();
            userSessionService.SetUser(context.Principal);
            return Task.CompletedTask;
        };

        options.Events.OnSigningOut = context =>
        {
            IUserSessionService userSessionService =
                context.HttpContext.RequestServices.GetRequiredService<IUserSessionService>();
            userSessionService.ClearUser();
            context.HttpContext.Session?.Clear();
            return Task.CompletedTask;
        };

        options.LogoutPath = "/logout";
        options.SlidingExpiration = true;
    }

    private static void ConfigureOpenIdConnect(OpenIdConnectOptions options, ConfigurationManager config, string secret)
    {
        IConfigurationSection oidcConfig = config.GetSection("Authentication:SRAM");

        options.Authority = oidcConfig["Authority"];
        options.ClientId = oidcConfig["ClientId"];
        options.ClientSecret = secret;
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
            string redirectUri = oidcConfig["RedirectUri"]!;
            if (context.Request.Query.TryGetValue("redirectUri", out StringValues redirectUriValue) &&
                redirectUriValue.Count > 0)
                redirectUri = redirectUriValue!;

            context.Response.Redirect(redirectUri);
            context.HandleResponse();
            return Task.CompletedTask;
        };
    }
}
