using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class NotesPage : ContentPage
{
    public NotesPage()
    {
        InitializeComponent();
        BindingContext = new NotesPageViewModel();
    }
}