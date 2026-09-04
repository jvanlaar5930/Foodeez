using Foodeez.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Foodeez.API.Filters;

/// <summary>
/// Rejects an authenticated request whose token carries no id we can read.
///
/// <c>[Authorize]</c> guarantees a valid signature and an unexpired token; it says nothing
/// about the claims inside. A token issued by an older version of this API, or by another
/// service sharing the signing key, can authenticate perfectly and still have no subject
/// claim - and every action that needs to know whose data to return would then be working
/// with a null id.
///
/// Twenty actions used to check for that themselves and return 401. Checking once, here,
/// means an action body can start with the work it is actually for, and means the check
/// cannot be forgotten on the next endpoint someone adds.
///
/// Anonymous requests pass through untouched: an endpoint that allows them has already said
/// it can cope without an id.
/// </summary>
public sealed class RequireTokenSubjectFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (CurrentUser.IdOf(context.HttpContext.User) is null)
        {
            context.Result = new UnauthorizedObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = "The access token does not identify a user. Sign in again.",
                Instance = context.HttpContext.Request.Path
            });
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
