using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromptBabbler.Api.Models.Responses;

namespace PromptBabbler.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api")]
public sealed class StatusController : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new StatusResponse
        {
            Status = "ok",
        });
    }
}
