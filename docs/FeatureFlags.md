The [Conflux.API]() project supports feature flags to enable/disable certain features.

## Configuration

Feature flags are configured in the `appsettings.json` file under the `FeatureFlags` section. Different environments can have different flag values by overriding them in environment-specific appsettings files (e.g., `appsettings.Development.json`).

## Available Feature Flags

| **Flag**                | **Function**                                                                                                                         | **Default** | **Notes** |
|-------------------------|--------------------------------------------------------------------------------------------------------------------------------------|:-----------:|-----------|
| Swagger                 | Enables the Swagger UI endpoint at `/swagger` for API documentation                                                                  |    true     | Should be `false` in production |
| SeedDatabase            | Seeds the database with NWOpen data if the database is not yet populated                                                            |    false    | Should be `false` in production |
| DatabaseConnection      | Enables database connection. If `false`, will not connect to a database                                                             |    true     | Set to `false` for testing without DB |
| SRAMAuthentication      | Enables SRAM (SURF Research Access Management) authentication                                                                       |    true     | Primary authentication method |
| OrcidAuthentication     | Enables ORCID authentication for linking                                                                                 |    true     | Cannot be enabled with SRAM in development |
| RAiDAuthentication      | Enables RAiD (Research Activity Identifier) authentication                                                                          |    false    | Requires RAiD credentials |
| OrcidIntegration        | Enables ORCID integration features for retrieving person information                                                                |    true     | Separate from authentication |
| ReverseProxy            | Configures forwarded headers for reverse proxy support                                                                              |    true     | Set to `true` when behind proxy/load balancer |
| HttpsRedirect           | Enables automatic redirection to HTTPS                                                                                              |    true     | Should be `true` in production |
| JsonIndentation         | Enables pretty-printing of JSON responses                                                                                           |    false    | Useful for development/debugging |
| SemanticSearch          | Enables semantic search functionality using embeddings                                                                              |    true     | Requires embedding model to be available |

## Environment-Specific Examples

### Development (`appsettings.Development.json`)
```json
{
  "FeatureFlags": {
    "Swagger": true,
    "SeedDatabase": true,
    "DatabaseConnection": true,
    "SRAMAuthentication": false,
    "OrcidAuthentication": false,
    "RAiDAuthentication": false,
    "ReverseProxy": false,
    "HttpsRedirect": false,
    "OrcidIntegration": true,
    "JsonIndentation": true,
    "SemanticSearch": true
  }
}
```

### Production (`appsettings.json`)
```json
{
  "FeatureFlags": {
    "Swagger": false,
    "SeedDatabase": false,
    "DatabaseConnection": true,
    "SRAMAuthentication": true,
    "OrcidAuthentication": false,
    "RAiDAuthentication": true,
    "ReverseProxy": true,
    "HttpsRedirect": true,
    "OrcidIntegration": true,
    "JsonIndentation": false,
    "SemanticSearch": true
  }
}
```

### Testing (`appsettings.Testing.json`)
```json
{
  "FeatureFlags": {
    "Swagger": true,
    "SeedDatabase": false,
    "DatabaseConnection": false,
    "SRAMAuthentication": false,
    "OrcidAuthentication": false,
    "RAiDAuthentication": false,
    "ReverseProxy": false,
    "HttpsRedirect": false,
    "OrcidIntegration": false,
    "JsonIndentation": false,
    "SemanticSearch": false
  }
}
```

## Usage in Code

Feature flags are accessed through the `IVariantFeatureManager` service:

```csharp
public class ExampleService
{
    private readonly IVariantFeatureManager _featureManager;

    public ExampleService(IVariantFeatureManager featureManager)
    {
        _featureManager = featureManager;
    }

    public async Task<string> DoSomethingAsync()
    {
        if (await _featureManager.IsEnabledAsync("SemanticSearch"))
        {
            // Use semantic search functionality
        }
        else
        {
            // Use fallback functionality
        }
    }
}
```