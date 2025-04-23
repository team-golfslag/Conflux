// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.API;

/// <summary>
/// The standard error response payload.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// The error message
    /// </summary>
    public string Error { get; init; } = string.Empty;
}
