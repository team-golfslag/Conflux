// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Models;
using Conflux.RepositoryConnections.SRAM.DTOs;
using Conflux.RepositoryConnections.SRAM.Models;

namespace Conflux.RepositoryConnections.SRAM;

public interface ICollaborationMapper
{
    Task<List<Collaboration>> Map(List<CollaborationDTO> collaborationDTOs);
    Task<List<Collaboration>> GetAllGroupsFromSCIMApi(List<CollaborationDTO> collaborationDtos);
    Task<Collaboration> GetCollaborationFromSCIMApi(CollaborationDTO collaborationDto);
    Task<Group> GetGroupFromSCIMApi(string groupUrn);
}
