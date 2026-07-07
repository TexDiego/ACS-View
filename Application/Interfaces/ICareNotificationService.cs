using ACS_View.Application.DTOs;

namespace ACS_View.Application.Interfaces;

public interface ICareNotificationService
{
    Task RefreshPregnancyNotificationsAsync();
    Task<IReadOnlyList<CareNotificationDto>> GetActiveAsync(int take = 10);
    Task<int> CountActiveAsync();
    Task ResolveAsync(int notificationId);
    Task DismissAsync(int notificationId);
    Task SnoozeAsync(int notificationId, DateTime snoozedUntil);
}
