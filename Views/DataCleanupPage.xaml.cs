using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class DataCleanupPage : ContentPage
{
    public DataCleanupPage(DataCleanupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
