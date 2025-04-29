// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Collections.Specialized;
using System.Security.Claims;
using System.Text.Json;
using Conflux.Data;
using Conflux.Domain;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Session;
using Conflux.Integrations.NWOpen;
using Conflux.Integrations.SRAM;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NWOpen.Net.Services;
using SRAM.SCIM.Net;
using SwaggerThemes;

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

    private static async Task ConfigureDatabase(WebApplicationBuilder builder, IVariantFeatureManager featureManager)
    {
        if (!await featureManager.IsEnabledAsync("DatabaseConnection"))
            return;

        builder.Services.AddHttpClient<INWOpenService, NWOpenService>();
        builder.Services.AddSingleton<TempProjectRetrieverService>();

        string connectionString = builder.Configuration.GetConnectionString("Database") ??
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
        builder.Services.AddScoped<ISRAMProjectSyncService, SRAMProjectSyncService>();
        builder.Services.AddScoped<ProjectsService>();
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
                OnRedirectToAuthorizationEndpoint = context =>
                {
                    // Get the *explicit* RedirectUri from configuration.
                    // This MUST exactly match one of the Redirect URIs registered with ORCID for your application.
                    string configuredRedirectUri = orcidConfig["RedirectUri"];

                    if (!string.IsNullOrEmpty(configuredRedirectUri))
                    {
                        // Set the RedirectUri property on the outgoing protocol message.
                        // The handler will use this value when communicating with ORCID.
                        // This ensures the redirect_uri parameter sent to ORCID matches your registration,
                        // especially important if the auto-generated one (based on request headers + CallbackPath)
                        // might differ due to proxy setups or specific host configurations.
                        context.RedirectUri = configuredRedirectUri;
                    }
                    // Allow the handler to complete the redirect. Do not manually redirect here.
                    return Task.CompletedTask;
                },
                OnCreatingTicket = context =>
                {
                    // Extract ORCID ID from token response
                    if (context.TokenResponse.Response.RootElement.TryGetProperty("orcid", out var orcidProp))
                    {
                        string? orcidId = orcidProp.GetString();
                        context.Identity?.AddClaim(new("sub", orcidId));
                        // set in session
                        context.HttpContext.Session.SetString("orcid", orcidId);
                    }

                    string finalRedirectUri = context.Properties.Items.TryGetValue("finalRedirect", out var redirect)
                        ? redirect as string ?? "/orcid/finalize"
                        : "/orcid/finalize";
                    context.Properties.Items["CustomRedirect"] =
                        $"/orcid/finalize?redirectUri={Uri.EscapeDataString(finalRedirectUri)}";

                    return Task.CompletedTask;
                },
                OnTicketReceived = context =>
                {
                    // Check if we have a custom redirect set in OnCreatingTicket
                    if (context.Properties.Items.TryGetValue("CustomRedirect", out var customRedirect))
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
        if (app.Environment.IsProduction())
        {
            app.UseForwardedHeaders();
        }

        if (await featureManager.IsEnabledAsync("Swagger"))
        {
            app.UseOpenApi();
            app.UseSwaggerUi(c =>
            {
                c.DocumentTitle = "Conflux API";
                c.CustomInlineStyles = SwaggerTheme.GetSwaggerThemeCss(Theme.UniversalDark);
            });
        }

        app.UseExceptionHandler(appBuilder =>
        {
            appBuilder.Run(async context =>
            {
                Exception? exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                switch (exception)
                {
                    case ProjectNotFoundException or ContributorNotFoundException:
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsJsonAsync(new ErrorResponse
                        {
                            Error = exception.Message,
                        });
                        break;
                    case ContributorAlreadyAddedToProjectException:
                        context.Response.StatusCode = 409;
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

        if (app.Environment.IsProduction())
        {
            app.UseHttpsRedirection();
        }


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

        if (!await featureManager.IsEnabledAsync("SeedDatabase") || await context.Projects.AnyAsync()) 
            return;

        TempProjectRetrieverService retriever = services.GetRequiredService<TempProjectRetrieverService>();
        SeedData seedData = retriever.MapProjectsAsync().Result;

        User devUser = UserSession.Development().User!;
        if (!await context.Users.AnyAsync(u => u.Id == devUser.Id))
            context.Users.Add(devUser);
        await context.Contributors.AddRangeAsync(seedData.Contributors);
        await context.Products.AddRangeAsync(seedData.Products);
        await context.Parties.AddRangeAsync(seedData.Parties);
        await context.Projects.AddRangeAsync(seedData.Projects);

        await context.SaveChangesAsync();
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
