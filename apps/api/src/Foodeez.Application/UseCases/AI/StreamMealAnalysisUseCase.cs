using System.Runtime.CompilerServices;
using System.Text;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.Interfaces.Services;

namespace Foodeez.Application.UseCases.AI;

/// <summary>
/// Analyses a meal that has not been saved yet, streaming the assessment as the model writes
/// it. Nothing is stored here - the caller keeps the finished analysis and sends it with the
/// meal when it is logged.
/// </summary>
public class StreamMealAnalysisUseCase
{
    private readonly IStreamingAIService _streaming;

    public StreamMealAnalysisUseCase(IStreamingAIService streaming)
    {
        _streaming = streaming;
    }

    public async IAsyncEnumerable<AIStreamEvent> ExecuteAsync(
        MealAnalysisRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var transcript = new StringBuilder();

        await foreach (var delta in AINarration.NarrateAsync(_streaming, MealAnalysisPrompt.Build(request), transcript, ct))
        {
            yield return AIStreamEvent.Delta(delta);
        }

        var analysis = MealAnalysisPrompt.Parse(transcript.ToString());

        yield return analysis == null
            ? AIStreamEvent.Error("The AI service could not analyze this meal right now. Please try again in a moment.")
            : AIStreamEvent.Result(analysis);
    }
}
