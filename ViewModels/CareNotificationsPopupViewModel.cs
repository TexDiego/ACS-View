using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.ViewModels;

public sealed class CareNotificationsPopupViewModel(
    ICareNotificationService notificationService,
    IPatientService patientService,
    IPersonsInfoPopupService personsInfoPopupService) : BaseViewModel
{
    public ObservableCollection<CareNotificationDto> Notifications { get; } = [];
    public bool HasNotifications => Notifications.Count > 0;
    public bool IsEmpty => !HasNotifications;

    public ICommand OpenPersonCommand => new Command<CareNotificationDto>(async item => await OpenPersonAsync(item));
    public ICommand SnoozeCommand => new Command<CareNotificationDto>(async item => await SnoozeAsync(item));
    public ICommand ResolveCommand => new Command<CareNotificationDto>(async item => await ResolveAsync(item));

    public async Task LoadAsync()
    {
        await notificationService.RefreshPregnancyNotificationsAsync();
        var notifications = await notificationService.GetActiveAsync(10);

        Notifications.Clear();
        foreach (var notification in notifications)
        {
            Notifications.Add(notification);
        }

        OnPropertyChanged(nameof(HasNotifications));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private async Task OpenPersonAsync(CareNotificationDto? item)
    {
        if (item?.Notification.PatientId is not int patientId || patientId <= 0)
        {
            return;
        }

        var patient = await patientService.GetPatientById(patientId);
        if (patient is not null)
        {
            await personsInfoPopupService.ShowAsync(patient);
        }
    }

    private async Task SnoozeAsync(CareNotificationDto? item)
    {
        if (item is null)
        {
            return;
        }

        await notificationService.SnoozeAsync(item.Notification.Id, DateTime.Today.AddDays(3));
        await LoadAsync();
    }

    private async Task ResolveAsync(CareNotificationDto? item)
    {
        if (item is null)
        {
            return;
        }

        await notificationService.ResolveAsync(item.Notification.Id);
        await LoadAsync();
    }
}
