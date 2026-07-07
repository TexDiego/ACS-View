using ACS_View.Application.DTOs;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.ViewModels;

public sealed class ActiveNoteNotificationsPopupViewModel : BaseViewModel
{
    public ActiveNoteNotificationsPopupViewModel(IEnumerable<ActiveNoteNotificationDto> notifications)
    {
        Notifications = new ObservableCollection<ActiveNoteNotificationDto>(
            notifications.OrderBy(notification => notification.NotifyOn));
        HasNotifications = Notifications.Count > 0;
        IsEmpty = !HasNotifications;
        SummaryText = Notifications.Count == 1
            ? "1 notificação ativa"
            : $"{Notifications.Count} notificações ativas";

        RescheduleCommand = new Command<ActiveNoteNotificationDto>(notification =>
            RequestClose?.Invoke(new NoteNotificationManagementRequestDto(
                notification.NoteId,
                NoteNotificationManagementAction.Reschedule)));

        CancelCommand = new Command<ActiveNoteNotificationDto>(notification =>
            RequestClose?.Invoke(new NoteNotificationManagementRequestDto(
                notification.NoteId,
                NoteNotificationManagementAction.Cancel)));
    }

    public ObservableCollection<ActiveNoteNotificationDto> Notifications { get; }
    public bool HasNotifications { get; }
    public bool IsEmpty { get; }
    public string SummaryText { get; }
    public double PopupWidth => DefaultPopupWidth;
    public ICommand RescheduleCommand { get; }
    public ICommand CancelCommand { get; }
    public Action<NoteNotificationManagementRequestDto>? RequestClose { get; set; }
}
