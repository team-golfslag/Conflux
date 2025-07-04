// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Conflux.API.Filters;
using Conflux.Data;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Logic.Services;
using Conflux.Integrations.Archive;
using Conflux.Integrations.NWOpen;
using Conflux.Integrations.RAiD;
using Conflux.Integrations.SRAM;
using Crossref.Net.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NWOpen.Net.Services;
using ORCID.Net.Services;
using RAiD.Net;
using ROR.Net.Services;
using SRAM.SCIM.Net;
using SwaggerThemes;
using Pgvector.EntityFrameworkCore;

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

        string? basePath = builder.Configuration["Application:PathBase"];
        if (!string.IsNullOrEmpty(basePath))
            app.UsePathBase(new(basePath));

        // Configure middleware
        await ConfigureMiddleware(app, featureManager);

        // Initialize database
        await InitializeDatabase(app, featureManager);

        await app.RunAsync();
    }

    private static async Task ConfigureServices(WebApplicationBuilder builder, IVariantFeatureManager featureManager)
    {
        bool jsonIndented = await featureManager.IsEnabledAsync("JsonIndentation");
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            options.JsonSerializerOptions.WriteIndented = jsonIndented;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerDocument(c => { c.Title = "Conflux API"; });

        builder.Services.AddDistributedMemoryCache();
        await ConfigureDatabase(builder, featureManager);

        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(20);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddScoped<IContributorsService, ContributorsService>();
        builder.Services.AddScoped<IProjectDescriptionsService, ProjectDescriptionsService>();
        builder.Services.AddScoped<IProjectTitlesService, ProjectTitlesService>();
        builder.Services.AddScoped<IPeopleService, PeopleService>();
        builder.Services.AddScoped<IProjectOrganisationsService, ProjectOrganisationsService>();
        builder.Services.AddScoped<ICollaborationMapper, CollaborationMapper>();
        builder.Services.AddScoped<IUserSessionService, UserSessionService>();
        builder.Services.AddScoped<ISessionMappingService, SessionMappingService>();
        builder.Services.AddScoped<ISRAMProjectSyncService, SRAMProjectSyncService>();
        builder.Services.AddScoped<IProjectMapperService, ProjectMapperService>();
        builder.Services.AddScoped<ProjectsService>();
        builder.Services.AddScoped<IProductsService, ProductsService>();
        builder.Services.AddScoped<IRAiDService, RAiDService>();
        builder.Services.AddScoped<IRaidInfoService, RaidInfoService>();
        builder.Services.AddScoped<IProjectsService, ProjectsService>();
        builder.Services.AddScoped<IAccessControlService, AccessControlService>();
        builder.Services.AddScoped<ITimelineService, TimelineService>();
        builder.Services.AddScoped<IAdminService, AdminService>();
        builder.Services.AddSingleton<ILanguageService, LanguageService>();
        builder.Services.AddSingleton<IEmbeddingService, OnnxEmbeddingService>();


        if (await featureManager.IsEnabledAsync("OrcidIntegration"))
            builder.Services.AddScoped<IPersonRetrievalService, PersonRetrievalService>(provider =>
            {
                IConfigurationSection orcidConfig = provider.GetRequiredService<IConfiguration>()
                    .GetSection("Authentication:Orcid");
                string? secret = Environment.GetEnvironmentVariable("ORCID_CLIENT_SECRET");
                if (string.IsNullOrEmpty(secret))
                    throw new InvalidOperationException("ORCID_CLIENT_SECRET not set.");
                PersonRetrievalServiceOptions options = new(
                    orcidConfig["Origin"],
                    orcidConfig["ClientId"],
                    secret);
                return new(options);
            });

        // Register the filter factory with scoped lifetime to match its dependencies
        builder.Services.AddScoped<AccessControlFilterFactory>();

        ConfigureRorServices(builder);
        ConfigureCrossrefServices(builder);
        ConfigureArchiveServices(builder);

        await ConfigureSRAMServices(builder, featureManager);
        await ConfigureRAiDServices(builder, featureManager);

        if (!await featureManager.IsEnabledAsync("ReverseProxy"))
            return;

        // Configure Forwarded Headers options
        // This helps the application correctly determine the client's scheme (http/https) and host
        // when running behind a reverse proxy (like Docker, Nginx, etc.)
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            // Forward the X-Forwarded-For (client IP) and X-Forwarded-Proto (http/https) headers
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // By default, only loopback proxies are trusted.
            // Clear these restrictions if your proxy is not on the same machine.
            // Be careful with this in production; ideally, configure KnownProxies/KnownNetworks.
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }

    private static void ConfigureCrossrefServices(WebApplicationBuilder builder)
    {
        builder.Services.Configure<CrossrefServiceOptions>(builder.Configuration.GetSection("ROR"));

        builder.Services.AddHttpClient("Crossref");
        builder.Services.AddScoped<ICrossrefService, CrossrefService>(provider =>
        {
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("Crossref");
            IOptions<CrossrefServiceOptions> optionsAccessor =
                provider.GetRequiredService<IOptions<CrossrefServiceOptions>>();
            ILogger<CrossrefService> logger = provider.GetRequiredService<ILogger<CrossrefService>>();

            return new CrossrefService(httpClient, optionsAccessor, logger);
        });
    }

    private static void ConfigureArchiveServices(WebApplicationBuilder builder)
    {
        string? accessKey = Environment.GetEnvironmentVariable("WEBARCHIVE_ACCESS_KEY");
        string? secretKey = Environment.GetEnvironmentVariable("WEBARCHIVE_SECRET_KEY");
        if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
            Console.WriteLine(
                "Warning: WEBARCHIVE_ACCESS_KEY and WEBARCHIVE_SECRET_KEY not set. Continuing without authentication.");

        builder.Services.AddHttpClient("WebArchive");
        builder.Services.AddScoped<IWebArchiveService, WebArchiveService>(provider =>
        {
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("WebArchive");
            if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
                httpClient.DefaultRequestHeaders.Authorization =
                    new("LOW", $"{accessKey}:{secretKey}");
            return new(httpClient);
        });
    }

    private static async Task ConfigureDatabase(WebApplicationBuilder builder, IVariantFeatureManager featureManager)
    {
        if (!await featureManager.IsEnabledAsync("DatabaseConnection"))
            return;

        builder.Services.AddHttpClient<INWOpenService, NWOpenService>();
        builder.Services.AddSingleton<TempProjectRetrieverService>();

        string connectionString = builder.Configuration.GetConnectionString("Database") ??
            ConnectionStringHelper.GetConnectionStringFromEnvironment();

        builder.Services.AddDbContext<ConfluxContext>(opt =>
            opt.UseNpgsql(connectionString,
                npgsql => npgsql.MigrationsAssembly("Conflux.Data")
                    .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                    .UseVector()));
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
    }

    private static async Task ConfigureRAiDServices(WebApplicationBuilder builder,
        IVariantFeatureManager featureManager)
    {
        builder.Services.Configure<RAiDServiceOptions>(
            builder.Configuration.GetSection("RAiD"));

        builder.Services.AddHttpClient("RAiD");

        bool raidEnabled = await featureManager.IsEnabledAsync("RAiDAuthentication");

        builder.Services.AddScoped<IRAiDService>(provider =>
        {
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>()
                .CreateClient("RAiD");
            IOptions<RAiDServiceOptions> optionsAccessor = provider.GetRequiredService<IOptions<RAiDServiceOptions>>();
            ILogger<RAiDService> logger = provider.GetRequiredService<ILogger<RAiDService>>();

            RAiDService raidSvc = new(httpClient, optionsAccessor, logger);

            string? raidUsername = Environment.GetEnvironmentVariable("RAID_USERNAME");
            string? raidPassword = Environment.GetEnvironmentVariable("RAID_PASSWORD");
            if ((string.IsNullOrEmpty(raidUsername) || string.IsNullOrEmpty(raidPassword)) && raidEnabled)
                throw new InvalidOperationException(
                    "RAID_USERNAME and RAID_PASSWORD must be set in environment variables.");

            raidSvc.SetUsernameAndPassword(raidUsername!, raidPassword!);
            return raidSvc;
        });
    }

    private static void ConfigureRorServices(WebApplicationBuilder builder)
    {
        builder.Services.Configure<OrganizationServiceOptions>(builder.Configuration.GetSection("ROR"));

        builder.Services.AddHttpClient("ROR");
        builder.Services.AddScoped<IOrganizationService, OrganizationService>(provider =>
        {
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("ROR");
            IOptions<OrganizationServiceOptions> optionsAccessor =
                provider.GetRequiredService<IOptions<OrganizationServiceOptions>>();
            ILogger<OrganizationService> logger = provider.GetRequiredService<ILogger<OrganizationService>>();

            return new OrganizationService(httpClient, optionsAccessor, logger);
        });
    }

    private static async Task ConfigureAuthentication(WebApplicationBuilder builder,
        IVariantFeatureManager featureManager)
    {
        AuthenticationBuilder authBuilder = builder.Services.AddAuthentication();
        bool sramEnabled = await featureManager.IsEnabledAsync("SRAMAuthentication");
        bool orcidEnabled = await featureManager.IsEnabledAsync("ORCIDAuthentication");

        // both enabled and development env
        if (sramEnabled && orcidEnabled && builder.Environment.IsDevelopment())
            throw new InvalidOperationException(
                "Both SRAM and ORCID authentication cannot be enabled at the same time in development.");

        // Add ORCID authentication regardless of which primary auth is used
        AddOrcidAuth(authBuilder, builder.Configuration);

        if (sramEnabled)
            SetupSRAMAuth(builder);
        else
            SetupDevelopmentAuth(builder);
    }

    private static void AddOrcidAuth(AuthenticationBuilder authBuilder, IConfiguration config)
    {
        IConfigurationSection orcidConfig = config.GetSection("Authentication:Orcid");
        IConfigurationSection appConfig = config.GetSection("Application");
        string basePath = appConfig["PathBase"]?.TrimEnd('/') ?? string.Empty;

        string? orcidSecret = Environment.GetEnvironmentVariable("ORCID_CLIENT_SECRET");
        if (string.IsNullOrEmpty(orcidSecret))
        {
            Console.WriteLine("Warning: ORCID_CLIENT_SECRET not set. ORCID integration disabled.");
            return;
        }

        // Configure cookies without explicit domain
        authBuilder.AddCookie("OrcidCookie", options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });

        authBuilder.AddOAuth("orcid", options =>
        {
            options.ClientId = orcidConfig["ClientId"];
            options.ClientSecret = orcidSecret;
            options.CallbackPath = orcidConfig["CallbackPath"];
            options.AuthorizationEndpoint = orcidConfig["AuthorizationEndpoint"];
            options.TokenEndpoint = orcidConfig["TokenEndpoint"];
            options.UserInformationEndpoint = orcidConfig["UserInformationEndpoint"];
            options.SignInScheme = "OrcidCookie";
            options.SaveTokens = true;

            // Configure correlation cookie without domain
            options.CorrelationCookie.SameSite = SameSiteMode.Lax;
            options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

            // Configure ORCID scope
            options.Scope.Clear();
            options.Scope.Add("/authenticate");

            // Map claims
            options.ClaimActions.Clear();
            options.ClaimActions.MapJsonKey("sub", "orcid");

            options.Events = new()
            {
                OnCreatingTicket = context =>
                {
                    // Extract ORCID ID from token response
                    if (context.TokenResponse.Response?.RootElement.TryGetProperty("orcid",
                        out JsonElement orcidProp) ?? false)
                    {
                        string? orcidId = orcidProp.GetString();
                        context.Identity?.AddClaim(new("sub", orcidId));
                        // set in session
                        context.HttpContext.Session.SetString("orcid", orcidId);
                    }

                    string finalRedirectUri =
                        context.Properties.Items.TryGetValue("finalRedirect", out string? redirect)
                            ? redirect ?? $"{basePath}/orcid/finalize"
                            : $"{basePath}/orcid/finalize";
                    context.Properties.Items["CustomRedirect"] =
                        $"{basePath}/orcid/finalize?redirectUri={Uri.EscapeDataString(finalRedirectUri)}";

                    return Task.CompletedTask;
                },
                OnTicketReceived = context =>
                {
                    // Check if we have a custom redirect set in OnCreatingTicket
                    if (context.Properties.Items.TryGetValue("CustomRedirect", out string? customRedirect))
                    {
                        context.Response.Redirect(customRedirect);
                        context.HandleResponse();
                    }

                    return Task.CompletedTask;
                },
            };
        });
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
        if (await featureManager.IsEnabledAsync("ReverseProxy"))
            app.UseForwardedHeaders();

        if (await featureManager.IsEnabledAsync("Swagger"))
        {
            app.UseOpenApi();
            app.UseSwaggerUi(c =>
            {
                c.DocumentTitle = "Conflux API";
                c.CustomInlineStyles = SwaggerTheme.GetSwaggerThemeCss(Theme.Monokai);
            });
        }

        app.UseExceptionHandler(appBuilder =>
        {
            appBuilder.Run(async context =>
            {
                Exception? exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                switch (exception)
                {
                    case ProjectNotFoundException
                         or ProjectDescriptionNotFoundException
                         or ProjectTitleNotFoundException
                         or ContributorNotFoundException
                         or ProductNotFoundException
                         or PersonNotFoundException
                         or OrganisationNotFoundException
                         or ProjectOrganisationNotFoundException:
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsJsonAsync(new ErrorResponse
                        {
                            Error = exception.Message,
                        });
                        break;
                    case PersonHasContributorsException
                         or ContributorAlreadyAddedToProjectException:
                        context.Response.StatusCode = 409;
                        await context.Response.WriteAsJsonAsync(new ErrorResponse
                        {
                            Error = exception.Message,
                        });
                        break;
                    case UnauthorizedAccessException
                         or ProjectAlreadyMintedException
                         or ProjectNotMintedException:
                        context.Response.StatusCode = 403;
                        await context.Response.WriteAsJsonAsync(new ErrorResponse
                        {
                            Error = exception.Message,
                        });
                        break;
                    case UserNotAuthenticatedException:
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsJsonAsync(new ErrorResponse
                        {
                            Error = exception.Message,
                        });
                        break;
                    default:
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsJsonAsync(new ErrorResponse
                        {
                            Error = "An unexpected error occurred.",
                        });
                        break;
                }
            });
        });

        if (await featureManager.IsEnabledAsync("HttpsRedirection"))
            app.UseHttpsRedirection();

        app.UseCors("AllowLocalhost");
        app.UseSession();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
    }

    private static async Task InitializeDatabase(WebApplication app, IVariantFeatureManager featureManager)
    {
        using IServiceScope scope = app.Services.CreateScope();
        IServiceProvider services = scope.ServiceProvider;

        if (services.GetService<ConfluxContext>() == null && !await featureManager.IsEnabledAsync("DatabaseConnection"))
            return;

        ConfluxContext context = services.GetRequiredService<ConfluxContext>();
        if (context.Database.IsRelational())
            await context.Database.MigrateAsync();

        IUserSessionService userSessionService = services.GetRequiredService<IUserSessionService>();
        await userSessionService.ConsolidateSuperAdmins();

        if (!await featureManager.IsEnabledAsync("SeedDatabase") || context.ShouldSeed())
            return;

        TempProjectRetrieverService retriever = services.GetRequiredService<TempProjectRetrieverService>();
        SeedData seedData = retriever.MapProjectsAsync().Result;

        await context.Users.AddRangeAsync(seedData.Users);
        await context.UserRoles.AddRangeAsync(seedData.UserRoles);
        await context.Contributors.AddRangeAsync(seedData.Contributors);
        await context.Products.AddRangeAsync(seedData.Products);
        await context.Organisations.AddRangeAsync(seedData.Organisations);
        await context.Projects.AddRangeAsync(seedData.Projects);
        await context.People.AddRangeAsync(seedData.People);

        await context.SaveChangesAsync();

        // Generate embeddings for all seeded projects
        IProjectsService projectsService = services.GetRequiredService<IProjectsService>();
        int embeddingUpdateCount = await projectsService.UpdateProjectEmbeddingsAsync();

        ILogger<Program> logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database seeded successfully. Generated embeddings for {Count} projects.",
            embeddingUpdateCount);
    }

    private static void SetupDevelopmentAuth(WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication("DevelopmentAuthScheme")
            .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthHandler>("DevelopmentAuthScheme", _ => { })
            .AddCookie(ConfigureCookieAuth);
    }

    private static void SetupSRAMAuth(WebApplicationBuilder builder)
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

    private static void ConfigureOpenIdConnect(
        OpenIdConnectOptions options,
        IConfiguration config,
        string secret)
    {
        IConfigurationSection oidcConfig = config.GetSection("Authentication:SRAM");
        IConfigurationSection appConfig = config.GetSection("Application");

        string baseUrl = appConfig["BaseUrl"]!.TrimEnd('/');
        string pathBase = appConfig["PathBase"]!.TrimEnd('/');
        string callbackPath = oidcConfig["CallbackPath"]!;
        string signoutPath = oidcConfig["SignoutPath"]!;

        options.Authority = oidcConfig["Authority"];
        options.ClientId = oidcConfig["ClientId"];
        options.ClientSecret = secret;
        options.CallbackPath = callbackPath;
        options.SignedOutCallbackPath = signoutPath;

        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;

        options.CorrelationCookie.Path = pathBase;
        options.NonceCookie.Path = pathBase;

        List<string>? scopes = oidcConfig.GetSection("Scopes").Get<List<string>>();
        if (scopes != null)
            foreach (string scope in scopes)
                options.Scope.Add(scope);

        Dictionary<string, string>? claimMappings =
            oidcConfig.GetSection("ClaimMappings").Get<Dictionary<string, string>>();
        if (claimMappings != null)
            foreach (KeyValuePair<string, string> mapping in claimMappings)
                options.ClaimActions.MapJsonKey(mapping.Key, mapping.Value);

        options.Events.OnRedirectToIdentityProvider = context =>
        {
            string redirectUri = $"{baseUrl}{pathBase}{callbackPath}";
            if (context.Request.Query.TryGetValue("redirectUri", out StringValues redirectUriValue) &&
                redirectUriValue.Count > 0)
                redirectUri = redirectUriValue!;

            context.ProtocolMessage.PostLogoutRedirectUri = redirectUri;
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToIdentityProviderForSignOut = context =>
        {
            context.ProtocolMessage.PostLogoutRedirectUri =
                $"{baseUrl}{pathBase}{signoutPath}";
            return Task.CompletedTask;
        };
    }
}
