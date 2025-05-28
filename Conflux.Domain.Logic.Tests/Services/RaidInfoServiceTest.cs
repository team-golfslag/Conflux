// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Integrations.RAiD;
using Microsoft.EntityFrameworkCore;
using Moq;
using RAiD.Net;
using Testcontainers.PostgreSql;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class RaidInfoServiceTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private ConfluxContext _context = null!;
    private ProjectMapperService _mapper = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        ConfluxContext context = new(options);
        await context.Database.EnsureCreatedAsync();
        _context = context;
        _mapper = new(context);
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }


    [Fact]
    public async Task MintRAiDAsync_MintsRAiD_WhenNoIncompatibilities()
    {
        Mock<IRAiDService> mock = new Mock<IRAiDService>();
        
        mock.Verify(s => s.MintRaidAsync(null), Times.Once);
        IRAiDService raidService = mock.Object;
        
    }
    
    
}
