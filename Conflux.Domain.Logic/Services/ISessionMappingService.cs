// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
//
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Models;

namespace Conflux.Domain.Logic.Services;

/// <summary>
/// Interface for mapping session data to the existing domain model
/// </summary>
public interface ISessionMappingService
{
    /// <summary>
    /// Collects all the data from the user session.
    /// </summary>
    /// <param name="userSession">The user session to collect the data from.</param>
    Task CollectSessionData(UserSession userSession);
}
