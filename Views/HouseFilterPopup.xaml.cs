using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class HouseFilterPopup : Popup<HouseNumberFilterDto>
{
    private readonly HouseFilterPopupViewModel _viewModel;
    private readonly IDialogService _dialogService;

    public HouseFilterPopup(HouseNumberFilterDto filter, IDialogService dialogService)
    {
        InitializeComponent();
        _dialogService = dialogService;
        BindingContext = _viewModel = new HouseFilterPopupViewModel(filter);
        Loaded += (_, _) => InputFocusGuard.ClearTextInputFocus(this);
    }

    private async void CancelButton_Clicked(object sender, EventArgs e) => await CloseAsync();

    private async void ApplyButton_Clicked(object sender, EventArgs e)
    {
        if (!_viewModel.TryCreateFilter(out var filter))
        {
            await _dialogService.ShowAlertAsync("Filtro inválido", _viewModel.ErrorMessage, "OK");
            return;
        }
        await CloseAsync(filter!);
    }
}
