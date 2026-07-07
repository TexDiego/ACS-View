using ACS_View.Application.Interfaces;
using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class CareNotificationsPopup : Popup
{
    public CareNotificationsPopup(
        ICareNotificationService notificationService,
        IPatientService patientService,
        IPersonsInfoPopupService personsInfoPopupService)
    {
        InitializeComponent();
        var viewModel = new CareNotificationsPopupViewModel(
            notificationService,
            patientService,
            personsInfoPopupService);
        BindingContext = viewModel;
        _ = viewModel.LoadAsync();
    }
}
