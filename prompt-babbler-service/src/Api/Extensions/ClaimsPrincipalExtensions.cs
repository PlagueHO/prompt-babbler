using System.Security.Claims;
using Microsoft.Identity.Web;
using PromptBabbler.Domain.Constants;

namespace PromptBabbler.Api.Extensions;

/// <summary>
/// Extension methods for <see cref="ClaimsPrincipal"/> to support both
/// authenticated (Entra ID) and anonymous single-user mode.
/// </summary>
internal static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Returns the Entra ID object ID from the authenticated user's claims,
    /// or <c>"_anonymous"</c> when authentication is disabled (single-user mode).
    /// </summary>
    public static string GetUserIdOrAnonymous(this ClaimsPrincipal user)
    {
        return user.GetObjectId() ?? UserIds.Anonymous;
    }
}
