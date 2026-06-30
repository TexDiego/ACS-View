namespace ACS_View.Application.DTOs;

public sealed record NoteNotificationRequestDto(DateTime NotifyOn, string Message);
