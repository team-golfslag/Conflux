// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Security.Claims;
using Conflux.Domain.Session;

namespace Conflux.Domain.Logic.Services;

public interface IUserSessionService
{
    Task<UserSession?> GetSession();
    Task<User> GetUser();
    Task CommitUser(UserSession userSession);
    Task<UserSession?> SetUser(ClaimsPrincipal? claims);
    Task ConsolidateSuperAdmins();
    void ClearUser();
}
