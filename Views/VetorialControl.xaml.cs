using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class VetorialControl : ContentPage
{
	public VetorialControl(VetorialControlViewModel viewModel)
	{
		BindingContext = viewModel;
        InitializeComponent();
	}
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        InputFocusGuard.ClearTextInputFocus(this);
        await ((VetorialControlViewModel)BindingContext).LoadAsync();
    }
    protected override void OnDisappearing()
    {
        ((VetorialControlViewModel)BindingContext).CancelPendingSearch();
        InputFocusGuard.ClearTextInputFocus(this);
        base.OnDisappearing();
    }
}