using ACS_View.Application.DTOs;
using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class AddNotificationPopup : Popup<NoteNotificationRequestDto>
{
    private readonly AddNotificationPopupViewModel _viewModel;

    public AddNotificationPopup(string noteContent, DateTime? activeNotificationDate)
    {
        InitializeComponent();
        BindingContext = _viewModel = new AddNotificationPopupViewModel(noteContent, activeNotificationDate);
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

    private async void CancelNotificationButton_Clicked(object sender, EventArgs e)
    {
        await CloseAsync(new NoteNotificationRequestDto(default, string.Empty, CancelExisting: true));
    }
}
