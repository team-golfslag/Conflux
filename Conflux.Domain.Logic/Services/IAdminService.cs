// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs.Responses;

namespace Conflux.Domain.Logic.Services;

public interface IAdminService
{
    public Task<List<UserResponseDTO>> GetUsersByQuery(string? query, bool adminsOnly);
    public Task<UserResponseDTO> SetUserTier(Guid userId, UserTier tier);
    public Task<List<string>> GetAvailableLectorates();
    public Task<List<string>> GetAvailableOrganisations();
    public Task<UserResponseDTO> AssignLectoratesToUser(Guid userId, List<string> lectorates);
    public Task<UserResponseDTO> AssignOrganisationsToUser(Guid userId, List<string> organisations);
}
