namespace MaintainOps.Domain.Exceptions;

/// <summary>
/// The request conflicts with the current state of the data: an invalid state transition,
/// a duplicate, or a stale edit. The API returns 409.
/// </summary>
/// <param name="code">Stable, machine-readable code the frontend can branch on, e.g. <c>workorder.invalid_transition</c>.</param>
/// <param name="message">Plain-language sentence shown to the user. No ids or technical detail.</param>
public sealed class ConflictException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
