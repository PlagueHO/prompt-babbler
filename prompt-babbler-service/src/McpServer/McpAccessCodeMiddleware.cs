using System.Security.Cryptography;
using System.Text;

namespace PromptBabbler.McpServer;

public sealed class McpAccessCodeMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private readonly string _accessCode = configuration["AccessControl:AccessCode"] ?? string.Empty;

    public async Task InvokeAsync(HttpContext context)
    {
        if (string.IsNullOrEmpty(_accessCode))
        {
            await next(context);
            return;
        }

        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (authHeader is null || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var providedCode = authHeader["Bearer ".Length..].Trim();
        var validCode = Encoding.UTF8.GetBytes(_accessCode);
        var provided = Encoding.UTF8.GetBytes(providedCode);

        if (!CryptographicOperations.FixedTimeEquals(validCode, provided))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }
}
