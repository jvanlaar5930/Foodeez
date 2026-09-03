using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.UseCases.MealLogs;

/// <summary>
/// Saved meals: the meals somebody eats over and over, kept under a name so logging them
/// again costs one tap and no guesswork.
/// </summary>
public class MealTemplatesUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>How many past meals to offer as "log this again". A screenful, not a history.</summary>
    private const int RecentLimit = 12;

    public MealTemplatesUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<MealTemplateDto>> ListAsync(Guid userId)
    {
        var templates = await _unitOfWork.MealTemplates.GetByUserAsync(userId);
        return templates.Select(MapTemplate).ToList();
    }

    /// <summary>
    /// Save the meal on screen under a name, replacing any meal already saved under it.
    /// Replacing rather than refusing is the kinder reading of the same gesture: someone
    /// saving "Usual breakfast" a second time is correcting the first one.
    /// </summary>
    public async Task<MealTemplateDto?> SaveAsync(SaveMealTemplateRequest request)
    {
        var name = request.Name.Trim();
        if (name.Length == 0 || request.Items.Count == 0)
        {
            return null;
        }

        var template = await _unitOfWork.MealTemplates.FindByNameAsync(request.UserId, name);
        var isNew = template is null;

        if (template is null)
        {
            template = new MealTemplate { UserId = request.UserId };
        }
        else
        {
            foreach (var stale in template.Items.ToList())
            {
                template.Items.Remove(stale);
                _unitOfWork.MealTemplates.RemoveItem(stale);
            }
        }

        template.Name = name;
        template.MealType = request.MealType;

        foreach (var itemRequest in request.Items)
        {
            var foodItem = await _unitOfWork.FoodItems.GetByIdAsync(itemRequest.FoodItemId);
            if (foodItem is null)
            {
                continue;
            }

            var item = new MealTemplateItem
            {
                MealTemplateId = template.Id,
                FoodItemId = foodItem.Id,
                Quantity = itemRequest.Quantity,
                Unit = itemRequest.Unit,
                NutritionalInfo = foodItem.NutritionalInfo.Scale(ScaleFor(foodItem, itemRequest.Quantity)),
                FoodItem = foodItem
            };

            template.Items.Add(item);

            // A brand-new template is saved as a whole graph, so its items come with it; items
            // added to one already on file have to go through the DbSet themselves.
            if (!isNew)
            {
                await _unitOfWork.MealTemplates.AddItemAsync(item);
            }
        }

        if (template.Items.Count == 0)
        {
            return null;
        }

        if (isNew)
        {
            await _unitOfWork.MealTemplates.AddAsync(template);
        }

        await _unitOfWork.SaveChangesAsync();
        return MapTemplate(template);
    }

    public async Task<bool> DeleteAsync(Guid templateId, Guid userId)
    {
        var template = await _unitOfWork.MealTemplates.GetDetailedByIdAsync(templateId);

        // Someone else's saved meal reads as missing rather than forbidden: confirming it
        // exists would tell an unauthorised caller something they should not learn.
        if (template is null || template.UserId != userId)
        {
            return false;
        }

        _unitOfWork.MealTemplates.Delete(template);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// The items of a saved meal, ready to drop into the meal dialog.
    ///
    /// Reaching for a saved meal is counted here rather than when the meal is finally saved:
    /// the count only decides what order these appear in, and asking the client to report
    /// back after saving would leave the ordering wrong whenever it forgot to.
    /// </summary>
    public async Task<QuickAddResultDto?> ApplyAsync(Guid templateId, Guid userId)
    {
        var template = await _unitOfWork.MealTemplates.GetDetailedByIdAsync(templateId);
        if (template is null || template.UserId != userId)
        {
            return null;
        }

        template.TimesUsed++;
        template.LastUsedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        return FromTemplate(template);
    }

    /// <summary>The user's last few meals, offered up for logging again as they stand.</summary>
    public async Task<List<RecentMealDto>> RecentAsync(Guid userId)
    {
        var logs = await _unitOfWork.MealLogs.GetRecentAsync(userId, RecentLimit);
        var savedSignatures = (await _unitOfWork.MealTemplates.GetByUserAsync(userId))
            .Select(Signature)
            .ToHashSet();

        var seen = new HashSet<string>();
        var recents = new List<RecentMealDto>();

        foreach (var log in logs)
        {
            if (log.Items.Count == 0)
            {
                continue;
            }

            // Eating the same breakfast five days running should offer it once, not five times.
            var signature = Signature(log);
            if (!seen.Add(signature))
            {
                continue;
            }

            recents.Add(new RecentMealDto
            {
                MealLogId = log.Id,
                LogDate = log.LogDate,
                MealType = log.MealType,
                Summary = string.Join(", ", log.Items.Select(i => i.FoodItem.Name)),
                Items = log.Items.Select(i => new MealTemplateItemDto
                {
                    FoodItem = FoodItemMapper.ToDto(i.FoodItem),
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    NutritionalInfo = FoodItemMapper.ToDto(i.NutritionalInfo)
                }).ToList(),
                TotalNutrition = FoodItemMapper.ToDto(log.TotalNutrition),
                IsSaved = savedSignatures.Contains(signature)
            });
        }

        return recents;
    }

    /// <summary>A saved meal as quick add reports it: exact items, nothing estimated.</summary>
    public static QuickAddResultDto FromTemplate(MealTemplate template) => new()
    {
        MealTemplateId = template.Id,
        MealTemplateName = template.Name,
        MealType = template.MealType,
        Items = template.Items.Select(i => new QuickAddItemDto
        {
            FoodItem = FoodItemMapper.ToDto(i.FoodItem),
            Quantity = i.Quantity,
            Unit = i.Unit,
            Source = QuickAddSource.Saved,
            Confidence = 1f
        }).ToList()
    };

    private static MealTemplateDto MapTemplate(MealTemplate template) => new()
    {
        Id = template.Id,
        Name = template.Name,
        MealType = template.MealType,
        TimesUsed = template.TimesUsed,
        LastUsedAt = template.LastUsedAt,
        Items = template.Items.Select(i => new MealTemplateItemDto
        {
            FoodItem = FoodItemMapper.ToDto(i.FoodItem),
            Quantity = i.Quantity,
            Unit = i.Unit,
            NutritionalInfo = FoodItemMapper.ToDto(i.NutritionalInfo)
        }).ToList(),
        TotalNutrition = FoodItemMapper.ToDto(template.TotalNutrition)
    };

    private static float ScaleFor(FoodItem foodItem, float quantity) =>
        foodItem.ServingSize > 0 ? quantity / foodItem.ServingSize : quantity;

    /// <summary>
    /// What makes two meals "the same meal" for the purpose of not offering it twice: the same
    /// foods in the same amounts, whatever order they happen to be stored in.
    /// </summary>
    private static string Signature(MealTemplate template) =>
        Signature(template.Items.Select(i => (i.FoodItemId, i.Quantity)));

    private static string Signature(Domain.Entities.MealLog log) =>
        Signature(log.Items.Select(i => (i.FoodItemId, i.Quantity)));

    private static string Signature(IEnumerable<(Guid FoodItemId, float Quantity)> items) =>
        string.Join("|", items
            .Select(i => $"{i.FoodItemId:N}:{i.Quantity:0.##}")
            .OrderBy(s => s, StringComparer.Ordinal));
}
