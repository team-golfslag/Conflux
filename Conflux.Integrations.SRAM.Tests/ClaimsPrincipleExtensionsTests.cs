// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Security.Claims;
using Conflux.Integrations.SRAM.DTOs;
using Conflux.Integrations.SRAM.Extensions;
using Xunit;

namespace Conflux.Integrations.SRAM.Tests;

public class ClaimsPrincipleExtensionsTests
{
    [Fact]
    public void GetClaimValue_WhenClaimExists_ReturnsValue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("TestType", "TestValue"),
        };
        ClaimsIdentity identity = new(claims);
        ClaimsPrincipal principal = new(identity);

        // Act
        string? result = principal.GetClaimValue("TestType");

        // Assert
        Assert.Equal("TestValue", result);
    }

    [Fact]
    public void GetClaimValue_WhenClaimDoesNotExist_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("OtherType", "TestValue"),
        };
        ClaimsIdentity identity = new(claims);
        ClaimsPrincipal principal = new(identity);

        // Act
        string? result = principal.GetClaimValue("TestType");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetClaimValue_WhenMultipleClaimsOfSameType_ReturnsFirstValue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("TestType", "FirstValue"),
            new("TestType", "SecondValue"),
        };
        ClaimsIdentity identity = new(claims);
        ClaimsPrincipal principal = new(identity);

        // Act
        string? result = principal.GetClaimValue("TestType");

        // Assert
        Assert.Equal("FirstValue", result);
    }

    [Fact]
    public void GetCollaborations_WhenNoRoleClaims_ReturnsEmptyList()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("OtherType", "SomeValue"),
        };
        ClaimsIdentity identity = new(claims);
        ClaimsPrincipal principal = new(identity);

        // Act
        var result = principal.GetCollaborations();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetCollaborations_WithSingleCollaborationWithGroup_ParsesCorrectly()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("Role", "urn:mace:surf.nl:sram:group:surf:project1:conflux-group1"),
        };
        ClaimsIdentity identity = new(claims);
        ClaimsPrincipal principal = new(identity);

        // Act
        var result = principal.GetCollaborations();

        // Assert
        Assert.Single(result);
        Assert.Equal("surf", result[0].Organization);
        Assert.Equal("project1", result[0].Name);
        Assert.Single(result[0].Groups);
        Assert.Equal("conflux-group1", result[0].Groups[0]);
    }

    [Fact]
    public void GetCollaborations_WithSingleCollaborationWithoutGroup_ParsesCorrectly()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("Role", "urn:mace:surf.nl:sram:group:surf:project1"),
        };
        ClaimsIdentity identity = new(claims);
        ClaimsPrincipal principal = new(identity);

        // Act
        var result = principal.GetCollaborations();

        // Assert
        Assert.Single(result);
        Assert.Equal("surf", result[0].Organization);
        Assert.Equal("project1", result[0].Name);
        Assert.Empty(result[0].Groups);
    }

    [Fact]
    public void GetCollaborations_WithMultipleGroupsInSameCollaboration_GroupsCorrectly()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("Role", "urn:mace:surf.nl:sram:group:surf:project1:conflux-group1"),
            new("Role", "urn:mace:surf.nl:sram:group:surf:project1:conflux-group2"),
            new("Role", "urn:mace:surf.nl:sram:group:surf:project1"),
        };
        ClaimsIdentity identity = new(claims);
        ClaimsPrincipal principal = new(identity);

        // Act
        var result = principal.GetCollaborations();

        // Assert
        Assert.Single(result);
        Assert.Equal("surf", result[0].Organization);
        Assert.Equal("project1", result[0].Name);
        Assert.Equal(2, result[0].Groups.Count);
        Assert.Contains("conflux-group1", result[0].Groups);
        Assert.Contains("conflux-group2", result[0].Groups);
    }

    [Fact]
    public void GetCollaborations_WithMultipleCollaborations_ParsesAllCorrectly()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("Role", "urn:mace:surf.nl:sram:group:surf:project1:conflux-group1"),
            new("Role", "urn:mace:surf.nl:sram:group:other:project2:conflux-group2"),
        };
        ClaimsIdentity identity = new(claims);
        ClaimsPrincipal principal = new(identity);

        // Act
        var result = principal.GetCollaborations();

        // Assert
        Assert.Equal(2, result.Count);

        CollaborationDTO? project1 = result.FirstOrDefault(c => c.Name == "project1");
        Assert.NotNull(project1);
        Assert.Equal("surf", project1.Organization);
        Assert.Single(project1.Groups);
        Assert.Equal("conflux-group1", project1.Groups[0]);

        CollaborationDTO? project2 = result.FirstOrDefault(c => c.Name == "project2");
        Assert.NotNull(project2);
        Assert.Equal("other", project2.Organization);
        Assert.Single(project2.Groups);
        Assert.Equal("conflux-group2", project2.Groups[0]);
    }

    [Fact]
    public void GetCollaborations_WithNonMatchingRoleClaims_IgnoresThem()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("Role", "urn:mace:surf.nl:sram:group:surf:project1:conflux-group1"),
            new("Role", "invalid-format"),
            new("Role", "urn:different:format"),
        };
        ClaimsIdentity identity = new(claims);
        ClaimsPrincipal principal = new(identity);

        // Act
        var result = principal.GetCollaborations();

        // Assert
        Assert.Single(result);
        Assert.Equal("surf", result[0].Organization);
        Assert.Equal("project1", result[0].Name);
        Assert.Single(result[0].Groups);
        Assert.Equal("conflux-group1", result[0].Groups[0]);
    }
}
