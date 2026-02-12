using ACS_View.MVVM.ViewModels;

namespace ACS_View.MVVM.Views;

public partial class NotesPage : ContentPage
{
    public NotesPage()
    {
        InitializeComponent();
        BindingContext = new NotesPageViewModel();
    }
}