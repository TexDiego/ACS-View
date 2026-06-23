using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class ImportDataPage : ContentPage
{
    public ImportDataPage(ImportDataViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
