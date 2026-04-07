using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PromptBabbler.Domain.Configuration;

namespace PromptBabbler.Api.Middleware;

public sealed class AccessCodeMiddleware
{
    private static readonly string[] AllowlistedPathPrefixes =
    [
        "/health",
        "/alive",
        "/api/config/access-status",
        "/api/error",
        "/openapi",
    ];

    private static readonly byte[] ErrorBody = JsonSerializer.SerializeToUtf8Bytes(new { error = "Access code required" });

    private readonly RequestDelegate _next;
    private readonly ILogger<AccessCodeMiddleware> _logger;

    public AccessCodeMiddleware(RequestDelegate next, ILogger<AccessCodeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IOptionsMonitor<AccessControlOptions> optionsMonitor)
    {
        var options = optionsMonitor.CurrentValue;

        if (string.IsNullOrEmpty(options.AccessCode))
        {
            await _next(context);
            return;
        }

        var requestPath = context.Request.Path.Value ?? string.Empty;

        if (IsAllowlisted(requestPath))
        {
            await _next(context);
            return;
        }

        var providedCode = context.Request.Headers["X-Access-Code"].FirstOrDefault();

        // WebSocket connections cannot set custom headers, so also accept the
        // access code as a query-string parameter (same pattern used for JWT
        // auth on the /api/transcribe/stream endpoint).
        if (string.IsNullOrEmpty(providedCode))
        {
            providedCode = context.Request.Query["access_code"].FirstOrDefault();
        }

        if (string.IsNullOrEmpty(providedCode) || !FixedTimeEquals(options.AccessCode, providedCode))
        {
            _logger.LogWarning("Access code validation failed for {Path}", requestPath);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.Body.WriteAsync(ErrorBody);
            return;
        }

        await _next(context);
    }

    private static bool IsAllowlisted(string path)
    {
        foreach (var prefix in AllowlistedPathPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool FixedTimeEquals(string expected, string actual)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
