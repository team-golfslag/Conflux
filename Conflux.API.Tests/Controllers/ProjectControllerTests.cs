using Conflux.API.Controllers;
using Conflux.API.DTOs;
using Conflux.API.Services;
using Conflux.Data;
using Conflux.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Conflux.API.Tests.Controllers;

public class ProjectControllerTests  : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private ConfluxContext _context;
    
    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        
        ConfluxContext context = new(options);
        await context.Database.EnsureCreatedAsync();    
        _context = context;
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task GetProjectById_ShouldReturnProject_WhenProjectExists()
    {
        // Arrange
        ProjectController projectController = new(_context);
        
        Guid projectId = Guid.NewGuid();
        
        // Insert a test project
        Project testProject = new()
        {
            Id = projectId,
            Title = "Test Title",
            Description = "Test Description",
            StartDate = new DateOnly(2023, 1, 1),
            EndDate = new DateOnly(2023, 12, 31),
        };

        _context.Projects.Add(testProject);
        await _context.SaveChangesAsync();
        
        // Act
        Project? project = (await projectController.GetProjectById(projectId)).Value;
        
        // Assert
        Assert.NotNull(project);
        Assert.Equal(testProject.Title, project.Title);
        Assert.Equal(testProject.Description, project.Description);
        Assert.Equal(testProject.StartDate, project.StartDate);
        Assert.Equal(testProject.EndDate, project.EndDate);
        
    }

    [Fact]
    public async Task GetProjectById_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        ProjectController projectController = new(_context);
        
        Guid projectId = Guid.NewGuid();
        
        // Act
        Project? project = (await projectController.GetProjectById(projectId)).Value;
        
        // Assert
        Assert.Null(project);
    }

    [Fact]
    public async Task CreateProject_ShouldCreateProject()
    {
        // Arrange
        ProjectController projectController = new(_context);
        // Create a test project
        ProjectDto testProject = new()
        {
            Title = "Test Title",
            Description = "Test Description",
            StartDate = new DateOnly(2023, 1, 1),
            EndDate = new DateOnly(2023, 12, 31),
        };
        
        // Act
        projectController.CreateProject(testProject);
        await _context.SaveChangesAsync();
        Project project = _context.Projects.Single(p => p.Id == testProject.Id);
        
        // Assert
        Assert.NotNull(project);
        Assert.Equal(testProject.Title, project.Title);
        Assert.Equal(testProject.Description, project.Description);
        Assert.Equal(testProject.StartDate, project.StartDate);
        Assert.Equal(testProject.EndDate, project.EndDate);
    }

    [Fact]
    public async Task UpdateProject_ShouldUpdateProject()
    {
        // Arrange
        ProjectController projectController = new(_context);

        Guid projectId = Guid.NewGuid();

        // Create a test project
        ProjectDto testProject = new()
        {
            Id = projectId,
            Title = "Test Title",
            Description = "Test Description",
            StartDate = new DateOnly(2023, 1, 1),
            EndDate = new DateOnly(2023, 12, 31),
        };

        projectController.CreateProject(testProject);
        await _context.SaveChangesAsync();
        Project project = _context.Projects.Single(p => p.Id == testProject.Id);

        // Create an updated project
        ProjectUpdateDto updatedTestProject = new()
        {
            Title = "Updated Title",
            Description = "Updated Description",
            StartDate = new DateOnly(2023, 1, 2),
            EndDate = new DateOnly(2023, 12, 30),
        };

        // Act
        ActionResult<Project> updatedProjectResult =
            await projectController.UpdateProject(projectId, updatedTestProject);
        Project? updatedProject = updatedProjectResult.Value;

        // Assert
        Assert.NotNull(updatedProject);
        Assert.Equal(updatedTestProject.Title, updatedProject.Title);
        Assert.Equal(updatedTestProject.Description, updatedProject.Description);
        Assert.Equal(updatedTestProject.StartDate, updatedProject.StartDate);
        Assert.Equal(updatedTestProject.EndDate, updatedProject.EndDate);
    }

    [Fact]
    public async Task UpdateProject_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        ProjectController projectController = new(_context);
        
        Guid projectId = Guid.NewGuid();
        
        // Create an updated project
        ProjectUpdateDto updatedTestProject = new()
        {
            Title = "Updated Title",
            Description = "Updated Description",
            StartDate = new DateOnly(2023, 1, 2),
            EndDate = new DateOnly(2023, 12, 30),
        };
        
        // Act
        ActionResult<Project> updatedProjectResult =
            await projectController.UpdateProject(projectId, updatedTestProject);
        
        // Assert
        Assert.Null(updatedProjectResult.Value);
    }

    [Fact]
    public async Task AddPersonToProject_ShouldReturnProject_WhenSucces()
    {
        // Arrange
        ProjectController projectController = new(_context);
        
        Guid projectId = Guid.NewGuid();
        Guid personId = Guid.NewGuid();
        
        // Create a test project
        ProjectDto testProject = new()
        {
            Id = projectId,
            Title = "Test Title",
            Description = "Test Description",
            StartDate = new DateOnly(2023, 1, 1),
            EndDate = new DateOnly(2023, 12, 31),
        };
        
        projectController.CreateProject(testProject);
        await _context.SaveChangesAsync();
        
        // Insert a test person
        Person testPerson = new()
        {
            Id = personId,
            Name = "Test Person",
        };

        _context.People.Add(testPerson);
        await _context.SaveChangesAsync();
        
        // Act
        ActionResult<Project> projectResult = await projectController.AddPersonToProject(projectId, testPerson.Id);
        Project? project = projectResult.Value;
        
        // Assert
        Assert.NotNull(project);
        Assert.IsType<OkObjectResult>(projectResult);
        Assert.Equal(project.People[0].Id, testPerson.Id);
        Assert.Equal(project.People[0].Name, testPerson.Name);
    }
    
    [Fact]
    public async Task AddPersonToProject_ShouldReturnNotFound_WhenPersonDoesNotExist()
    {
        // Arrange
        ProjectController projectController = new(_context);
        
        Guid projectId = Guid.NewGuid();
        Guid personId = Guid.NewGuid();
        
        // Create a test project
        ProjectDto testProject = new()
        {
            Id = projectId,
            Title = "Test Title",
            Description = "Test Description",
            StartDate = new DateOnly(2023, 1, 1),
            EndDate = new DateOnly(2023, 12, 31),
        };
        
        var res = projectController.CreateProject(testProject);
        
        await _context.SaveChangesAsync();
        
        // Act
        ActionResult<Project> projectResult = await projectController.AddPersonToProject(projectId, personId);
        
        // Assert
        Assert.IsType<NotFoundObjectResult>(projectResult);
    }
    
    [Fact]
    public async Task AddPersonToProject_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        ProjectController projectController = new(_context);
        
        Guid projectId = Guid.NewGuid();
        Guid personId = Guid.NewGuid();
        
        // Insert a test person
        Person testPerson = new()
        {
            Id = personId,
            Name = "Test Person",
        };

        _context.People.Add(testPerson);
        await _context.SaveChangesAsync();
        
        // Act
        ActionResult<Project> projectResult = await projectController.AddPersonToProject(projectId, personId);
        
        // Assert
        Assert.IsType<NotFoundObjectResult>(projectResult);
    }
    
    [Fact]
    public async Task AddPersonToProject_ShouldReturnBadRequest_WhenPersonAlreadyAdded()
    {
        // Arrange
        ProjectController projectController = new(_context);
        
        Guid projectId = Guid.NewGuid();
        Guid personId = Guid.NewGuid();
        
        // Create a test project
        ProjectDto testProjectDto = new()
        {
            Title = "Test Title",
            Description = "Test Description",
            StartDate = new DateOnly(2023, 1, 1),
            EndDate = new DateOnly(2023, 12, 31),
        };
        
        //Project testProject = projectController.CreateProject(testProjectDto);
        await _context.SaveChangesAsync();
        
        // Insert a test person
        Person testPerson = new()
        {
            Id = personId,
            Name = "Test Person",
        };

        _context.People.Add(testPerson);
        await _context.SaveChangesAsync();
        
        // Act
        ActionResult<Project> projectResult = await projectController.AddPersonToProject(projectId , personId);        
        ActionResult<Project> projectResult1 = await projectController.AddPersonToProject(projectId, personId);
        
        // Assert
        Assert.NotNull(projectResult);
        Assert.Null(projectResult.Value);
        BadRequestResult? badRequestResult = projectResult.Result as BadRequestResult;
        Assert.NotNull(badRequestResult);
        Assert.Equal(400, badRequestResult.StatusCode);

    }
}
