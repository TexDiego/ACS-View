namespace ACS_View.Application.DTOs;

public sealed record NoteNotificationManagementRequestDto(
    int NoteId,
    NoteNotificationManagementAction Action);

public enum NoteNotificationManagementAction
{
    Reschedule,
    Cancel
}
