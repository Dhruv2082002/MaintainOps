using Microsoft.AspNetCore.Mvc;

namespace MaintainOps.Api.Controllers;

/// <summary>
/// Placeholder endpoint so the API has something to call while Foundations is being built.
/// </summary>
[ApiController]
[Route("")]
public sealed class RootController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { name = "MaintainOps" });
}
