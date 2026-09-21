using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceUsers.Services;
using StudentPass.Contracts;

namespace ServiceUsers.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class HealthController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok", service = "StudentPass API" });
}
