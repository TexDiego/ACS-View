using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class ForgotPassword : ContentPage
{
    public ForgotPassword(ForgotPasswordViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
