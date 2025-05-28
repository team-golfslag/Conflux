// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.Exceptions;

public class ContributorAlreadyExistsException(Guid contributorDtoProjectId, Guid personId)
    : Exception($"Contributor with project ID {contributorDtoProjectId} and person ID {personId} already exists.");
