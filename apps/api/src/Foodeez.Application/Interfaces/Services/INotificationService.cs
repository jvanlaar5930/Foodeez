namespace Foodeez.Application.Interfaces.Services;

public interface INotificationService
{
    Task SendWeightCheckInReminderAsync(Guid userId);
    Task SendMealTrackingReminderAsync(Guid userId, string userName);
}
