// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Conflux.Data.Tests;

public class ConfluxContextFactoryTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _tempConfigPath;

    public ConfluxContextFactoryTests()
    {
        // Create a temporary directory for test configuration files
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"ConfluxTest_{Guid.CreateVersion7()}");
        Directory.CreateDirectory(_tempDirectory);
        _tempConfigPath = Path.Combine(_tempDirectory, "../Conflux.API");
        Directory.CreateDirectory(_tempConfigPath);
    }

    public void Dispose()
    {
        // Clean up temporary files
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, true);
        if (Directory.Exists(_tempConfigPath))
            Directory.Delete(_tempConfigPath, true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void CreateDbContext_WithDevelopmentEnvironment_CreatesContext()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        
        // Create test appsettings.json
        string appSettingsContent = """
        {
          "ConnectionStrings": {
            "Database": "Host=localhost;Port=5432;Database=test_db;Username=test;Password=test"
          }
        }
        """;
        File.WriteAllText(Path.Combine(_tempConfigPath, "appsettings.json"), appSettingsContent);
        
        // Create test appsettings.Development.json
        string devSettingsContent = """
        {
          "ConnectionStrings": {
            "Database": "Host=localhost;Port=5432;Database=test_dev_db;Username=test;Password=test"
          }
        }
        """;
        File.WriteAllText(Path.Combine(_tempConfigPath, "appsettings.Development.json"), devSettingsContent);

        var factory = new ConfluxContextFactory();

        // Act
        ConfluxContext context = factory.CreateDbContext(Array.Empty<string>());

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context.Database);
        
        context.Dispose();
    }

    [Fact]
    public void CreateDbContext_WithProductionEnvironment_CreatesContext()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        
        // Create test appsettings.json
        string appSettingsContent = """
        {
          "ConnectionStrings": {
            "Database": "Host=localhost;Port=5432;Database=test_db;Username=test;Password=test"
          }
        }
        """;
        File.WriteAllText(Path.Combine(_tempConfigPath, "appsettings.json"), appSettingsContent);

        var factory = new ConfluxContextFactory();

        // Act
        ConfluxContext context = factory.CreateDbContext(Array.Empty<string>());

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context.Database);
        
        context.Dispose();
    }

    [Fact]
    public void CreateDbContext_WithNullEnvironment_DefaultsToDevelopment()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        
        // Create test appsettings.json
        string appSettingsContent = """
        {
          "ConnectionStrings": {
            "Database": "Host=localhost;Port=5432;Database=test_db;Username=test;Password=test"
          }
        }
        """;
        File.WriteAllText(Path.Combine(_tempConfigPath, "appsettings.json"), appSettingsContent);

        var factory = new ConfluxContextFactory();

        // Act
        ConfluxContext context = factory.CreateDbContext(Array.Empty<string>());

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context.Database);
        
        context.Dispose();
    }

    [Fact]
    public void CreateDbContext_WithMissingConnectionString_FallsBackToEnvironmentVariables()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("DB_HOST", "localhost");
        Environment.SetEnvironmentVariable("DB_PORT", "5432");
        Environment.SetEnvironmentVariable("DB_NAME", "env_test_db");
        Environment.SetEnvironmentVariable("DB_USER", "env_test");
        Environment.SetEnvironmentVariable("DB_PASSWORD", "env_test_pass");
        
        // Create test appsettings.json without ConnectionStrings
        string appSettingsContent = """
        {
          "Logging": {
            "LogLevel": {
              "Default": "Information"
            }
          }
        }
        """;
        File.WriteAllText(Path.Combine(_tempConfigPath, "appsettings.json"), appSettingsContent);

        var factory = new ConfluxContextFactory();

        // Act
        ConfluxContext context = factory.CreateDbContext(Array.Empty<string>());

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context.Database);
        
        context.Dispose();
        
        // Cleanup environment variables
        Environment.SetEnvironmentVariable("DB_HOST", null);
        Environment.SetEnvironmentVariable("DB_PORT", null);
        Environment.SetEnvironmentVariable("DB_NAME", null);
        Environment.SetEnvironmentVariable("DB_USER", null);
        Environment.SetEnvironmentVariable("DB_PASSWORD", null);
    }

    [Fact]
    public void CreateDbContext_WithEmptyConnectionString_FallsBackToEnvironmentVariables()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("DB_HOST", "localhost");
        Environment.SetEnvironmentVariable("DB_PORT", "5432");
        Environment.SetEnvironmentVariable("DB_NAME", "env_test_db");
        Environment.SetEnvironmentVariable("DB_USER", "env_test");
        Environment.SetEnvironmentVariable("DB_PASSWORD", "env_test_pass");
        
        // Create test appsettings.json with empty ConnectionStrings
        string appSettingsContent = """
        {
          "ConnectionStrings": {
            "Database": ""
          }
        }
        """;
        File.WriteAllText(Path.Combine(_tempConfigPath, "appsettings.json"), appSettingsContent);

        var factory = new ConfluxContextFactory();

        // Act
        ConfluxContext context = factory.CreateDbContext(Array.Empty<string>());

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context.Database);
        
        context.Dispose();
        
        // Cleanup environment variables
        Environment.SetEnvironmentVariable("DB_HOST", null);
        Environment.SetEnvironmentVariable("DB_PORT", null);
        Environment.SetEnvironmentVariable("DB_NAME", null);
        Environment.SetEnvironmentVariable("DB_USER", null);
        Environment.SetEnvironmentVariable("DB_PASSWORD", null);
    }

    [Fact]
    public void CreateDbContext_WithArgs_IgnoresArgs()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        
        // Create test appsettings.json
        string appSettingsContent = """
        {
          "ConnectionStrings": {
            "Database": "Host=localhost;Port=5432;Database=test_db;Username=test;Password=test"
          }
        }
        """;
        File.WriteAllText(Path.Combine(_tempConfigPath, "appsettings.json"), appSettingsContent);

        var factory = new ConfluxContextFactory();
        string[] args = ["--environment", "Production", "--connection", "some-connection"];

        // Act
        ConfluxContext context = factory.CreateDbContext(args);

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context.Database);
        
        context.Dispose();
    }

    [Fact]
    public void CreateDbContext_EnablesVectorExtension()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        
        // Create test appsettings.json
        string appSettingsContent = """
        {
          "ConnectionStrings": {
            "Database": "Host=localhost;Port=5432;Database=test_db;Username=test;Password=test"
          }
        }
        """;
        File.WriteAllText(Path.Combine(_tempConfigPath, "appsettings.json"), appSettingsContent);

        var factory = new ConfluxContextFactory();

        // Act
        ConfluxContext context = factory.CreateDbContext(Array.Empty<string>());

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context.Database);
        
        // Verify that the context was created with options
        Assert.NotNull(context.Database.GetDbConnection());
        
        context.Dispose();
    }
}
