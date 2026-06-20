using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class Profile : ContentPage
{
    public Profile(ProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
