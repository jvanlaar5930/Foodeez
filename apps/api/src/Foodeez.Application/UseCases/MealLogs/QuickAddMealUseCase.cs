using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.Interfaces.Services;

namespace Foodeez.Application.UseCases.MealLogs;

/// <summary>
/// Log a meal by describing it - "turkey sandwich on rye with mayo, and an apple" - instead of
/// searching out and adding every part of it one at a time.
///
/// Nothing here writes a meal log. The answer is a list of items for the user to look over in
/// the meal dialog, where they can be corrected, removed or added to before anything is saved.
/// </summary>
public class QuickAddMealUseCase
{
    private readonly IAIService _ai;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ParsedMealResolver _resolver;

    /// <summary>Longer than this is a diary entry, not a meal; the model is not asked to read it.</summary>
    private const int MaxDescriptionLength = 1000;

    public QuickAddMealUseCase(IAIService ai, IUnitOfWork unitOfWork, ParsedMealResolver resolver)
    {
        _ai = ai;
        _unitOfWork = unitOfWork;
        _resolver = resolver;
    }

    public async Task<QuickAddResultDto> ExecuteAsync(QuickAddRequest request, CancellationToken ct = default)
    {
        var description = request.Description.Trim();
        if (description.Length == 0)
        {
            return new QuickAddResultDto { Note = "Describe the meal and it will be broken into its parts." };
        }

        if (description.Length > MaxDescriptionLength)
        {
            description = description[..MaxDescriptionLength];
        }

        // A saved meal under exactly this name answers for free, exactly, and instantly. Only
        // an exact name is accepted: substituting a saved meal for something merely similar
        // would quietly log food the user did not describe.
        var saved = await _unitOfWork.MealTemplates.FindByNameAsync(request.UserId, description);
        if (saved is not null && saved.Items.Count > 0)
        {
            saved.TimesUsed++;
            saved.LastUsedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
            return MealTemplatesUseCase.FromTemplate(saved);
        }

        var parsed = await _ai.ParseMealDescriptionAsync(description, ct);
        var result = await _resolver.ResolveAsync(request.UserId, parsed, ct);

        if (result.Items.Count == 0)
        {
            result.Note ??= "No foods could be picked out of that. Try naming them more plainly, "
                + "for example \"2 slices rye bread, 100g turkey breast, 1 tbsp mayo\".";
        }

        return result;
    }
}
