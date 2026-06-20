using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class NotesPage : ContentPage
{
    private readonly NotesPageViewModel _viewModel;

    public NotesPage(NotesPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = _viewModel = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.LoadNotesAsync();
    }
}
