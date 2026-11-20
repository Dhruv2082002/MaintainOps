namespace MaintainOps.Application.Exceptions;

/// <summary>
/// The resource doesn't exist, or it belongs to another tenant (reported the same way so its
/// existence isn't revealed). The API returns 404.
/// </summary>
/// <param name="code">Stable, machine-readable code the frontend can branch on, e.g. <c>site.not_found</c>.</param>
/// <param name="message">Plain-language sentence shown to the user. No ids or technical detail.</param>
public sealed class NotFoundException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
