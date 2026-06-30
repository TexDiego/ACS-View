using ACS_View.Application.DTOs;
using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class AddNotificationPopup : Popup<NoteNotificationRequestDto>
{
    private readonly AddNotificationPopupViewModel _viewModel;

    public AddNotificationPopup(string noteContent)
    {
        InitializeComponent();
        BindingContext = _viewModel = new AddNotificationPopupViewModel(noteContent);
    }

    private async void CancelButton_Clicked(object sender, EventArgs e)
    {
        await CloseAsync();
    }

    private async void SaveButton_Clicked(object sender, EventArgs e)
    {
        if (!_viewModel.TryCreateRequest(out var request) || request is null)
        {
            return;
        }

        await CloseAsync(request);
    }
}
