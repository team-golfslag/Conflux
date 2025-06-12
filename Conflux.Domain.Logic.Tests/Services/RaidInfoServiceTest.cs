// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Conflux.Integrations.RAiD;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RAiD.Net;
using RAiD.Net.Domain;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class RaidInfoServiceTest : IDisposable
{
    private ConfluxContext _context = null!;
    private ProjectMapperService _mapper = null!;

    public RaidInfoServiceTest()
    {
        ServiceProvider serviceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.CreateVersion7()}")
            .UseInternalServiceProvider(serviceProvider)
            .Options;

        Mock<ILanguageService> languageServiceMock = new();
        languageServiceMock
            .Setup(s => s.IsValidLanguageCode(It.IsAny<string>()))
            .Returns(true);
        
        ConfluxContext context = new(options);
        context.Database.EnsureCreated();
        _context = context;
        _mapper = new(context, languageServiceMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task MintRAiDAsync_MintsRAiD_WhenNoIncompatibilities()
    {
        Guid projectId = await CreateUnmintedProject();

        Mock<IRAiDService> mockRaidService = new();
        Mock<IProjectMapperService> mockMapper = new();

        IRAiDService raidService = mockRaidService.Object;

        mockRaidService.Setup(s => s.MintRaidAsync(It.IsAny<RAiDCreateRequest>()))
            .ReturnsAsync(new RAiDDto
            {
                TraditionalKnowledgeLabel = null,
                Metadata = null,
                Identifier = new()
                {
                    IdValue = "https://raid.org/0.10.0.3/8767542",
                    SchemaUri = null,
                    RegistrationAgency = new()
                    {
                        Id = "https://ror.org/04pp8hn57",
                        SchemaUri = null,
                    },
                    Owner = new()
                    {
                        Id = "https://ror.org/04pp8hn57",
                        SchemaUri = null,
                        ServicePoint = 1,
                    },
                    RaidAgencyUrl = null,
                    License = null,
                    Version = 0,
                },
                Title = null,
                Date = null,
                Description = null,
                Access = null,
                AlternateUrl = null,
                Contributor = null,
                Organisation = null,
                Subject = null,
                RelatedRaid = null,
                RelatedObject = null,
                AlternateIdentifier = null,
                SpatialCoverage = null,
            });

        mockMapper.Setup(m => m.CheckProjectCompatibility(projectId))
            .ReturnsAsync([]);

        mockMapper.Setup(m => m.MapProjectCreationRequest(projectId)).ReturnsAsync(new RAiDCreateRequest
        {
            Metadata = null,
            Identifier = null,
            Title = null,
            Date = null,
            Description = null,
            Access = null,
            AlternateUrl = null,
            Contributor = null,
            Organisation = null,
            Subject = null,
            RelatedRaid = null,
            RelatedObject = null,
            AlternateIdentifier = null,
            SpatialCoverage = null,
        });

        mockMapper.Setup(m => m.MapProjectUpdateRequest(projectId)).ReturnsAsync(new RAiDUpdateRequest
        {
            Metadata = null,
            Identifier = null,
            Title = null,
            Date = null,
            Description = null,
            Access = null,
            AlternateUrl = null,
            Contributor = null,
            Organisation = null,
            Subject = null,
            RelatedRaid = null,
            RelatedObject = null,
            AlternateIdentifier = null,
            SpatialCoverage = null,
        });

        IProjectMapperService mapper = mockMapper.Object;


        RaidInfoService service = new(_context, raidService, mapper);

        await service.MintRAiDAsync(projectId);
        mockRaidService.Verify(s => s.MintRaidAsync(It.IsAny<RAiDCreateRequest>()), Times.Once);
    }

    private async Task<Guid> CreateUnmintedProject()
    {
        Guid projectId = Guid.CreateVersion7();

        _context.Projects.Add(new()
        {
            Id = projectId,
            SCIMId = null,
            RAiDInfo = null,
            StartDate = default,
            EndDate = null,
            Users = [],
            Contributors = [],
            Products = [],
            Organisations = [],
            Titles = [],
            Descriptions = [],
            LastestEdit = default,
        });
        await _context.SaveChangesAsync();
        return projectId;
    }

    private async Task<Guid> CreateMintedProject()
    {
        Guid projectId = Guid.CreateVersion7();

        _context.Projects.Add(new()
        {
            Id = projectId,
            SCIMId = null,
            RAiDInfo = new()
            {
                LatestSync = DateTime.UtcNow,
                Dirty = false,
                RAiDId = "https://raid.org/0.10.0.2/9786532",
                RegistrationAgencyId = "https://ror.org/98763",
                OwnerId = "2343",
                OwnerServicePoint = 18763,
                Version = 1,
            },
            StartDate = default,
            EndDate = null,
            Users = [],
            Contributors = [],
            Products = [],
            Organisations = [],
            Titles = [],
            Descriptions = [],
            LastestEdit = default,
        });
        await _context.SaveChangesAsync();
        return projectId;
    }

    [Fact]
    public async Task MintRAiDAsync_ThrowsException_WhenProjectNotCompatible()
    {
        Guid projectId = await CreateUnmintedProject();

        Mock<IProjectMapperService> mapperMock = new();
        Mock<IRAiDService> raidMock = new();

        mapperMock.Setup(m => m.CheckProjectCompatibility(projectId))
            .ReturnsAsync([
                new RAiDIncompatibility
                {
                    Type = RAiDIncompatibilityType.NoActivePrimaryTitle,
                    ObjectId = Guid.Empty,
                },
            ]);

        RaidInfoService service = new(_context, raidMock.Object, mapperMock.Object);
        await Assert.ThrowsAsync<ProjectNotRaidCompatibleException>(async () => await service.MintRAiDAsync(projectId));
        raidMock.Verify(r => r.MintRaidAsync(It.IsAny<RAiDCreateRequest>()), Times.Never);
    }

    [Fact]
    public async Task SyncRAiDAsync_ThrowsException_WhenProjectNotCompatible()
    {
        Guid projectId = await CreateMintedProject();

        Mock<IProjectMapperService> mapperMock = new();
        Mock<IRAiDService> raidMock = new();

        mapperMock.Setup(m => m.CheckProjectCompatibility(projectId))
            .ReturnsAsync([
                new RAiDIncompatibility
                {
                    Type = RAiDIncompatibilityType.NoActivePrimaryTitle,
                    ObjectId = Guid.Empty,
                },
            ]);

        RaidInfoService service = new(_context, raidMock.Object, mapperMock.Object);
        await Assert.ThrowsAsync<ProjectNotRaidCompatibleException>(async () => await service.SyncRAiDAsync(projectId));
        raidMock.Verify(r => r.UpdateRaidAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RAiDUpdateRequest>()),
            Times.Never);
    }

    [Fact]
    public async Task SyncRAiDAsync_UpdatesRAiD_WhenNoIncompatibilities()
    {
        Guid projectId = await CreateMintedProject();

        Mock<IRAiDService> mockRaidService = new();
        Mock<IProjectMapperService> mockMapper = new();

        IRAiDService raidService = mockRaidService.Object;

        mockRaidService.Setup(s => s.UpdateRaidAsync("0.10.0.2", "9786532", It.IsAny<RAiDUpdateRequest>()))
            .ReturnsAsync(new RAiDDto
            {
                TraditionalKnowledgeLabel = null,
                Metadata = null,
                Identifier = new()
                {
                    IdValue = "https://raid.org/0.10.0.3/8767542",
                    SchemaUri = null,
                    RegistrationAgency = new()
                    {
                        Id = "https://ror.org/04pp8hn57",
                        SchemaUri = null,
                    },
                    Owner = new()
                    {
                        Id = "https://ror.org/04pp8hn57",
                        SchemaUri = null,
                        ServicePoint = 1,
                    },
                    RaidAgencyUrl = null,
                    License = null,
                    Version = 0,
                },
                Title = null,
                Date = null,
                Description = null,
                Access = null,
                AlternateUrl = null,
                Contributor = null,
                Organisation = null,
                Subject = null,
                RelatedRaid = null,
                RelatedObject = null,
                AlternateIdentifier = null,
                SpatialCoverage = null,
            });

        mockMapper.Setup(m => m.CheckProjectCompatibility(projectId))
            .ReturnsAsync([]);

        mockMapper.Setup(m => m.MapProjectUpdateRequest(projectId)).ReturnsAsync(new RAiDUpdateRequest
        {
            Metadata = null,
            Identifier = null,
            Title = null,
            Date = null,
            Description = null,
            Access = null,
            AlternateUrl = null,
            Contributor = null,
            Organisation = null,
            Subject = null,
            RelatedRaid = null,
            RelatedObject = null,
            AlternateIdentifier = null,
            SpatialCoverage = null,
        });

        RaidInfoService service = new(_context, mockRaidService.Object, mockMapper.Object);

        await service.SyncRAiDAsync(projectId);
        mockRaidService.Verify(s => s.UpdateRaidAsync("0.10.0.2", "9786532", It.IsAny<RAiDUpdateRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task MintRAiDAsync_ThrowsException_WhenAlreadyMinted()
    {
        Guid projectId = await CreateMintedProject();

        Mock<IRAiDService> mockRaidService = new();
        Mock<IProjectMapperService> mockMapper = new();

        RaidInfoService service = new(_context, mockRaidService.Object, mockMapper.Object);

        await Assert.ThrowsAsync<ProjectAlreadyMintedException>(async () => await service.MintRAiDAsync(projectId));
        mockRaidService.Verify(s => s.MintRaidAsync(It.IsAny<RAiDCreateRequest>()), Times.Never);
    }

    [Fact]
    public async Task SyncRAiDAsync_ThrowsException_WhenNotMinted()
    {
        Guid projectId = await CreateUnmintedProject();

        Mock<IRAiDService> mockRaidService = new();
        Mock<IProjectMapperService> mockMapper = new();

        RaidInfoService service = new(_context, mockRaidService.Object, mockMapper.Object);

        await Assert.ThrowsAsync<ProjectNotMintedException>(async () => await service.SyncRAiDAsync(projectId));
        mockRaidService.Verify(
            s => s.UpdateRaidAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RAiDUpdateRequest>()), Times.Never);
    }

    [Fact]
    public async Task GetRAiDInfoByProjectId_ReturnsInfo_WhenMinted()
    {
        Guid projectId = await CreateMintedProject();

        Mock<IRAiDService> mockRaidService = new();
        Mock<IProjectMapperService> mockMapper = new();

        mockMapper.Setup(m => m.MapProjectUpdateRequest(projectId)).ReturnsAsync(new RAiDUpdateRequest
        {
            Metadata = null,
            Identifier = null,
            Title = null,
            Date = null,
            Description = null,
            Access = null,
            AlternateUrl = null,
            Contributor = null,
            Organisation = null,
            Subject = null,
            RelatedRaid = null,
            RelatedObject = null,
            AlternateIdentifier = null,
            SpatialCoverage = null,
        });

        RaidInfoService service = new(_context, mockRaidService.Object, mockMapper.Object);

        RAiDInfoResponseDTO result = await service.GetRAiDInfoByProjectId(projectId);

        Assert.Equal(projectId, result.projectId);
    }

    [Fact]
    public async Task GetRAiDInfoByProjectId_ThrowsException_WhenNotMinted()
    {
        Guid projectId = await CreateUnmintedProject();

        Mock<IRAiDService> mockRaidService = new();
        Mock<IProjectMapperService> mockMapper = new();

        RaidInfoService service = new(_context, mockRaidService.Object, mockMapper.Object);

        await Assert.ThrowsAsync<ProjectNotMintedException>(async () =>
            await service.GetRAiDInfoByProjectId(projectId));
    }

    [Fact]
    public async Task GetRAiDIncompatibilities_ReturnsIncompatibilities_WhenProjectMinted()
    {
        Guid projectId = await CreateUnmintedProject();

        Mock<IRAiDService> mockRaidService = new();
        Mock<IProjectMapperService> mockMapper = new();

        Guid testId = Guid.CreateVersion7();
        mockMapper.Setup(m => m.CheckProjectCompatibility(projectId))
            .ReturnsAsync([
                new()
                {
                    Type = RAiDIncompatibilityType.NoActivePrimaryTitle,
                    ObjectId = testId,
                },
            ]);

        RaidInfoService service = new(_context, mockRaidService.Object, mockMapper.Object);

        List<RAiDIncompatibility> result = await service.GetRAiDIncompatibilities(projectId);

        Assert.Single(result);
        Assert.Equal(RAiDIncompatibilityType.NoActivePrimaryTitle, result[0].Type);
        Assert.Equal(testId, result[0].ObjectId);
    }
}
