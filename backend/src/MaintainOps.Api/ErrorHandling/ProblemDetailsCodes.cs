namespace MaintainOps.Api.ErrorHandling;

/// <summary>
/// The <c>code</c> field every error response carries, so the frontend can branch on it
/// without reading the human-readable text.
/// </summary>
internal static class ProblemDetailsCodes
{
    public const string ExtensionName = "code";

    public const string UnexpectedError = "unexpected_error";
}
