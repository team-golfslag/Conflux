// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Security.Claims;
using Conflux.Domain.Models;

namespace Conflux.Domain.Logic.Services;

public interface IUserSessionService
{
    Task<UserSession?> GetUser();
    Task<UserSession?> UpdateUser();
    Task CommitUser(UserSession userSession);
    Task<UserSession?> SetUser(ClaimsPrincipal? claims);
    void ClearUser();
}
