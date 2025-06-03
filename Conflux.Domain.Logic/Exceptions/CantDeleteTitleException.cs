// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.Exceptions;

public class CantDeleteTitleException(Guid titleId)
    : Exception($"Can't delete title with id {titleId} because it is too old or has already been succeeded.")
{
}
