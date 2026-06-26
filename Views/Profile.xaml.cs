using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class Profile : ContentPage
{
    private readonly ProfileViewModel _viewModel;

    public Profile(ProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadProfileCommand.Execute(null);
    }
}
