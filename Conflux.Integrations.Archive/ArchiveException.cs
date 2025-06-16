// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Integrations.Archive;

public class ArchiveException : Exception 
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ArchiveException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ArchiveException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
