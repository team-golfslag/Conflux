// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Integrations.RAiD;
using Microsoft.EntityFrameworkCore;
using RAiD.Net;
using RAiD.Net.Domain;

namespace Conflux.Domain.Logic.Services;

public class RaidInfoService : IRaidInfoService
{
    private readonly ConfluxContext _context;
    private readonly IProjectMapperService _projectMapperService;
    private readonly IRAiDService _raidService;

    public RaidInfoService(ConfluxContext context, IRAiDService raidService, IProjectMapperService projectMapperService)
    {
        _context = context;
        _raidService = raidService;
        _projectMapperService = projectMapperService;
    }

    public async Task<RAiDInfoResponseDTO> GetRAiDInfoByProjectId(Guid projectId)
    {
        await VerifyProjectExists(projectId);

        RAiDInfo? info = await GetEntityAsync(projectId);
        if (info == null) throw new ProjectNotMintedException(projectId);

        return MapRAiDInfo(info);
    }

    public async Task<List<RAiDIncompatibility>> GetRAiDIncompatibilities(Guid projectId)
    {
        await VerifyProjectExists(projectId);

        List<RAiDIncompatibility> incompatibilities = await _projectMapperService.CheckProjectCompatibility(projectId);

        return incompatibilities;
    }

    public async Task<RAiDInfoResponseDTO> MintRAiDAsync(Guid projectId)
    {
        await VerifyProjectExists(projectId);

        RAiDInfo? raidInfo = await GetEntityAsync(projectId);
        if (raidInfo != null) throw new ProjectAlreadyMintedException(projectId);

        List<RAiDIncompatibility> incompatibilities =
            await _projectMapperService.CheckProjectCompatibility(projectId);
        if (incompatibilities.Count != 0) throw new ProjectNotRaidCompatibleException(projectId);

        RAiDCreateRequest dto = await _projectMapperService.MapProjectCreationRequest(projectId);
        RAiDDto? result = await _raidService.MintRaidAsync(dto);

        if (result == null) throw new RAiDException("An unexpected error occured. :(");

        RAiDInfo newRAiDInfo = GetNewRAiDInfo(projectId, result);
        _context.RAiDInfos.Update(newRAiDInfo);
        await _context.SaveChangesAsync();

        return MapRAiDInfo(newRAiDInfo);
    }


    public async Task<RAiDInfoResponseDTO> SyncRAiDAsync(Guid projectId)
    {
        await VerifyProjectExists(projectId);

        RAiDInfo? info = await GetEntityAsync(projectId);
        if (info == null) throw new ProjectNotMintedException(projectId);

        List<RAiDIncompatibility> incompatibilities = await _projectMapperService.CheckProjectCompatibility(projectId);
        if (incompatibilities.Count != 0) throw new ProjectNotRaidCompatibleException(projectId);

        RAiDUpdateRequest dto = await _projectMapperService.MapProjectUpdateRequest(projectId);

        (string prefix, string suffix) = GetRAiDPartsFromId(info.RAiDId);
        RAiDDto? result = await _raidService.UpdateRaidAsync(prefix, suffix, dto);
        if (result == null) throw new RAiDException("An unexpected error occured. :(");

        RAiDInfo newRaidInfo = GetNewRAiDInfo(projectId, result);
        _context.RAiDInfos.Update(newRaidInfo);
        await _context.SaveChangesAsync();

        return MapRAiDInfo(newRaidInfo);
    }

    private static (string, string) GetRAiDPartsFromId(string raidId)
    {
        // We parse a string of the form
        string[] parts = raidId.Split('/');
        int n = parts.Length;
        if (parts.Last().Length == 0)
            return (parts[n - 3], parts[n - 2]);
        return (parts[n - 2], parts[n - 1]);
    }

    private async Task<RAiDInfo?> GetEntityAsync(Guid projectId) => await _context.RAiDInfos.FindAsync(projectId);

    private static RAiDInfo GetNewRAiDInfo(Guid projectId, RAiDDto dto) =>
        new()
        {
            projectId = projectId,
            LatestSync = DateTime.UtcNow,
            Dirty = false,
            RAiDId = dto.Identifier.IdValue,
            RegistrationAgencyId = dto.Identifier.RegistrationAgency.Id,
            OwnerId = dto.Identifier.Owner.Id,
            OwnerServicePoint = dto.Identifier.Owner.ServicePoint,
            Version = dto.Identifier.Version,
        };

    private static RAiDInfoResponseDTO MapRAiDInfo(RAiDInfo info) =>
        new()
        {
            projectId = info.projectId,
            LatestSync = info.LatestSync,
            Dirty = info.Dirty,
            RAiDId = info.RAiDId,
            RegistrationAgencyId = info.RegistrationAgencyId,
            OwnerId = info.OwnerId,
            OwnerServicePoint = info.OwnerServicePoint,
            Version = info.Version,
        };

    private async Task VerifyProjectExists(Guid projectId)
    {
        bool projectExists = await _context.Projects.AnyAsync(p => p.Id == projectId);

        if (!projectExists) throw new ProjectNotFoundException(projectId);
    }
}
