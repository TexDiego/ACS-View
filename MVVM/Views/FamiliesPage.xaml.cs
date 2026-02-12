using ACS_View.MVVM.ViewModels;

namespace ACS_View.MVVM.Views;

public partial class FamiliesPage : ContentPage
{
    public FamiliesPage(int id)
	{
        InitializeComponent();
        BindingContext = new FamiliesViewModel(id);
    }
}