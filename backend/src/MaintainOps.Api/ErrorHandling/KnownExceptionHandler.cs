using MaintainOps.Application.Exceptions;
using MaintainOps.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MaintainOps.Api.ErrorHandling;

/// <summary>
/// Turns our own expected-failure exceptions into ProblemDetails responses.
/// Anything else returns <c>false</c>, so the built-in exception handler logs it and returns a 500.
/// Claiming every exception here would hide real crashes: since .NET 10 a handled exception isn't logged.
/// </summary>
internal sealed class KnownExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        (int Status, string Title, string Code)? known = exception switch
        {
            NotFoundException e => (StatusCodes.Status404NotFound, "We couldn't find what you were looking for.", e.Code),
            ConflictException e => (StatusCodes.Status409Conflict, "This conflicts with the current data.", e.Code),
            BusinessRuleException e => (StatusCodes.Status422UnprocessableEntity, "This action isn't allowed.", e.Code),
            _ => null,
        };

        if (known is not { } failure)
        {
            return false;
        }

        httpContext.Response.StatusCode = failure.Status;

        var problem = new ProblemDetails
        {
            Status = failure.Status,
            Title = failure.Title,
            Detail = exception.Message,
        };
        problem.Extensions[ProblemDetailsCodes.ExtensionName] = failure.Code;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
