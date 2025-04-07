// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Data.Tests;

public class ConfluxContextFactoryTests : IDisposable
{
    private readonly string? _originalHost;
    private readonly string? _originalName;
    private readonly string? _originalPassword;
    private readonly string? _originalPort;
    private readonly string? _originalUser;

    public ConfluxContextFactoryTests()
    {
        // Store original environment variables
        _originalHost = Environment.GetEnvironmentVariable("DB_HOST");
        _originalPort = Environment.GetEnvironmentVariable("DB_PORT");
        _originalName = Environment.GetEnvironmentVariable("DB_NAME");
        _originalUser = Environment.GetEnvironmentVariable("DB_USER");
        _originalPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

        // Set test environment variables
        Environment.SetEnvironmentVariable("DB_HOST", "testhost");
        Environment.SetEnvironmentVariable("DB_PORT", "1234");
        Environment.SetEnvironmentVariable("DB_NAME", "testdb");
        Environment.SetEnvironmentVariable("DB_USER", "testuser");
        Environment.SetEnvironmentVariable("DB_PASSWORD", "testpassword");
    }

    public void Dispose()
    {
        // Restore original environment variables
        Environment.SetEnvironmentVariable("DB_HOST", _originalHost);
        Environment.SetEnvironmentVariable("DB_PORT", _originalPort);
        Environment.SetEnvironmentVariable("DB_NAME", _originalName);
        Environment.SetEnvironmentVariable("DB_USER", _originalUser);
        Environment.SetEnvironmentVariable("DB_PASSWORD", _originalPassword);
    }

    [Fact]
    public void GetConnectionStringFromEnvironment_ReturnsCorrectConnectionString()
    {
        // Arrange
        string expected = "Host=testhost:1234;Database=testdb;Username=testuser;Password=testpassword";

        // Act
        string actual = ConfluxContextFactory.GetConnectionStringFromEnvironment();

        // Assert
        Assert.Equal(expected, actual);
    }
}
