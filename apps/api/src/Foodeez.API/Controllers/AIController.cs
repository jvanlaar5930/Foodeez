using Foodeez.Application.DTOs.AI;
using Foodeez.Application.UseCases.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Foodeez.API.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly GetDietaryRecommendationsUseCase _getRecommendationsUseCase;

    public AIController(GetDietaryRecommendationsUseCase getRecommendationsUseCase)
    {
        _getRecommendationsUseCase = getRecommendationsUseCase;
    }

    /// <summary>Get AI-powered dietary recommendations based on user profile and recent nutrition history.</summary>
    [HttpPost("recommendations")]
    [ProducesResponseType(typeof(DietaryRecommendationsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecommendations([FromBody] GetRecommendationsRequest request)
    {
        var recommendations = await _getRecommendationsUseCase.ExecuteAsync(request.UserId);
        return Ok(recommendations);
    }
}

public class GetRecommendationsRequest
{
    public Guid UserId { get; set; }
}
