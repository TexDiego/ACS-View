using ACS_View.Application.DTOs;
using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class ActiveNoteNotificationsPopup : Popup<NoteNotificationManagementRequestDto>
{
    public ActiveNoteNotificationsPopup(IEnumerable<ActiveNoteNotificationDto> notifications)
    {
        InitializeComponent();

        var viewModel = new ActiveNoteNotificationsPopupViewModel(notifications);
        viewModel.RequestClose = async request => await CloseAsync(request);
        BindingContext = viewModel;
    }

    private async void CloseButton_Clicked(object sender, EventArgs e)
    {
        await CloseAsync();
    }
}
