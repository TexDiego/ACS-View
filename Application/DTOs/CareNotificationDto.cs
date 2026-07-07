using ACS_View.Domain.Entities;

namespace ACS_View.Application.DTOs;

public sealed class CareNotificationDto
{
    public CareNotification Notification { get; set; } = new();
    public string PatientName { get; set; } = string.Empty;
    public string PriorityText { get; set; } = string.Empty;
    public string CategoryText { get; set; } = string.Empty;
    public string DueText { get; set; } = string.Empty;
}
