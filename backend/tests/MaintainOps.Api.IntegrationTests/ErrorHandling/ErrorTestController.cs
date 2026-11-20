using MaintainOps.Application.Exceptions;
using MaintainOps.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace MaintainOps.Api.IntegrationTests.ErrorHandling;

/// <summary>
/// Exists only in the test project. Each action fails in one way, so the tests can check
/// the response the API's error handling produces for it.
/// </summary>
[ApiController]
[Route("test/errors")]
public sealed class ErrorTestController : ControllerBase
{
    public const string NotFoundMessage = "That site doesn't exist.";
    public const string ConflictMessage = "This work order is on hold, so it can't be completed.";
    public const string BusinessRuleMessage = "The reviewer must be someone other than the person who completed the work.";
    public const string CrashMessage = "Secret internal detail that must never reach the client";

    [HttpGet("not-found")]
    public IActionResult ThrowNotFound() => throw new NotFoundException("test.not_found", NotFoundMessage);

    [HttpGet("conflict")]
    public IActionResult ThrowConflict() => throw new ConflictException("test.conflict", ConflictMessage);

    [HttpGet("business-rule")]
    public IActionResult ThrowBusinessRule() => throw new BusinessRuleException("test.business_rule", BusinessRuleMessage);

    [HttpGet("crash")]
    public IActionResult ThrowUnexpected() => throw new InvalidOperationException(CrashMessage);
}
