using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PromptBabbler.Domain.Configuration;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Api.Controllers;

[ApiController]
[Route("api/config")]
public sealed class ConfigController : ControllerBase
{
    private readonly IOptionsMonitor<AccessControlOptions> _accessControlOptions;

    public ConfigController(IOptionsMonitor<AccessControlOptions> accessControlOptions)
    {
        _accessControlOptions = accessControlOptions;
    }

    [HttpGet("access-status")]
    public IActionResult GetAccessStatus()
    {
        var options = _accessControlOptions.CurrentValue;
        return Ok(new AccessControlStatusResponse
        {
            AccessCodeRequired = !string.IsNullOrEmpty(options.AccessCode),
        });
    }
}
