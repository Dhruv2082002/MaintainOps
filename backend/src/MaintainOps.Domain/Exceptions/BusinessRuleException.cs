namespace MaintainOps.Domain.Exceptions;

/// <summary>
/// The input is well-formed and the current state allows the action, but a business rule refuses it.
/// The API returns 422.
/// </summary>
/// <param name="code">Stable, machine-readable code the frontend can branch on, e.g. <c>workorder.reviewer_must_differ</c>.</param>
/// <param name="message">Plain-language sentence shown to the user. No ids or technical detail.</param>
public sealed class BusinessRuleException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
