using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Foodeez.API.Controllers;

/// <summary>
/// The signed-in user's id, from the token rather than from the request.
///
/// Endpoints that take a userId in the query string are trusting the caller to say who they
/// are; for a private conversation and a personal shopping list that is not good enough, so
/// these read it from the subject claim instead.
/// </summary>
internal static class CurrentUser
{
    public static Guid? IdOf(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
