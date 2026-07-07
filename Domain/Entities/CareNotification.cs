using ACS_View.Domain.Enums;
using SQLite;

namespace ACS_View.Domain.Entities;

public class CareNotification
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int UserId { get; set; }

    [Indexed]
    public int? PatientId { get; set; }

    public CareNotificationCategory Category { get; set; }
    public CareNotificationType Type { get; set; }
    public CareNotificationPriority Priority { get; set; }
    public CareNotificationStatus Status { get; set; } = CareNotificationStatus.Active;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? DismissedAt { get; set; }
    public DateTime? SnoozedUntil { get; set; }

    [Indexed]
    public string UniqueKey { get; set; } = string.Empty;

    public string SourceRule { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = string.Empty;
}
