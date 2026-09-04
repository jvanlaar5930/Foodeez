using Microsoft.AspNetCore.Mvc;

namespace Foodeez.API.Controllers;

/// <summary>
/// The base every Foodeez controller derives from, so that "who is asking" is answered the
/// same way everywhere.
///
/// Before this, five controllers carried a private copy of the subject-claim lookup - one of
/// them (Admin) returning <c>Guid.Empty</c> for a token it could not read, where the others
/// returned null - and roughly twenty actions opened with
/// <c>if (CurrentUser.IdOf(User) is not { } userId) return Unauthorized();</c>. Under
/// <c>[Authorize]</c> that branch cannot be taken: a request without a valid token never
/// reaches the action. <see cref="Filters.RequireTokenSubjectFilter"/> now covers the one
/// case it was really guarding against - a token that authenticates but carries no usable
/// subject - so <see cref="UserId"/> can simply be a Guid.
/// </summary>
[ApiController]
public abstract class FoodeezController : ControllerBase
{
    /// <summary>
    /// The signed-in user's id, taken from the token rather than from the request. Endpoints
    /// that read it from the query string are trusting the caller to say who they are.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">
    /// If the action allows anonymous callers, or somehow ran without the filter. Use
    /// <see cref="UserIdOrNull"/> on endpoints that are reachable without a token.
    /// </exception>
    protected Guid UserId =>
        CurrentUser.IdOf(User)
        ?? throw new UnauthorizedAccessException(
            "This endpoint requires a signed-in user. If it is meant to allow anonymous " +
            "callers, read UserIdOrNull instead of UserId.");

    /// <summary>
    /// The signed-in user's id, or null when the caller is anonymous. For the endpoints that
    /// serve both - a public recipe list that highlights your own recipes when you are signed
    /// in, for instance.
    /// </summary>
    protected Guid? UserIdOrNull => CurrentUser.IdOf(User);

    /// <summary>
    /// The body for a request this API is refusing, in the one shape it uses everywhere.
    ///
    /// There used to be three: a bare string, <c>{ message }</c>, and <c>{ detail }</c> -
    /// alongside the ProblemDetails that <see cref="Middleware.ExceptionHandlingMiddleware"/>
    /// and <c>[ApiController]</c>'s own validation already return. Clients therefore had to
    /// guess. The mobile client reads <c>detail</c> and <c>title</c> only, so all seven
    /// <c>{ message }</c> refusals reached it as the generic "something went wrong" - the
    /// reason a rejected meal-plan slot said nothing about why.
    /// </summary>
    protected ProblemDetails Failure(string detail, int status = StatusCodes.Status400BadRequest) => new()
    {
        Status = status,
        Title = status == StatusCodes.Status409Conflict ? "Conflict" : "Request Rejected",
        Detail = detail,
        Instance = Request.Path
    };
}
