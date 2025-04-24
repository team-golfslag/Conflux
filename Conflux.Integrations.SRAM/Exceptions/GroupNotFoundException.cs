// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Integrations.SRAM.Exceptions;

public class GroupNotFoundException : Exception
{
    public GroupNotFoundException(string exceptionMessage)
        : base(exceptionMessage)
    {
    }

    public GroupNotFoundException(string exceptionMessage, Exception innerException)
        : base(exceptionMessage, innerException)
    {
    }
}
