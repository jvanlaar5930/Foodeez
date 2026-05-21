using Foodeez.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Foodeez.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public async Task SendWeightCheckInReminderAsync(Guid userId)
    {
        // TODO: Implement push notification via Firebase Cloud Messaging (FCM) or Expo Push Notifications.
        // Steps:
        //   1. Look up the user's push notification token from the database.
        //   2. Build an FCM/Expo notification payload with the reminder message.
        //   3. POST to the FCM/Expo endpoint with the payload.
        //   4. Handle delivery failures and token refresh.

        _logger.LogInformation("Weight check-in reminder queued for user {UserId}.", userId);

        await Task.CompletedTask;
    }

    public async Task SendMealTrackingReminderAsync(Guid userId, string userName)
    {
        // TODO: Implement push notification via Firebase Cloud Messaging (FCM) or Expo Push Notifications.
        // Steps:
        //   1. Look up the user's push notification token from the database.
        //   2. Build a personalized notification: "Hey {userName}, don't forget to log your meals today!"
        //   3. POST to the FCM/Expo endpoint with the payload.
        //   4. Handle delivery failures and token refresh.

        _logger.LogInformation("Meal tracking reminder queued for user {UserId} ({UserName}).", userId, userName);

        await Task.CompletedTask;
    }
}
