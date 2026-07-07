namespace ACS_View.Application.DTOs;

public sealed class ActiveNoteNotificationDto
{
    public int NoteId { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTime NotifyOn { get; init; }
    public string NotifyOnText => NotifyOn.ToString("dd/MM/yyyy HH:mm");
}
