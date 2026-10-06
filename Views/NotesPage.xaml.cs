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
        InputFocusGuard.ClearTextInputFocus(this);
        _ = _viewModel.LoadNotesAsync();
    }

    protected override void OnDisappearing()
    {
        InputFocusGuard.ClearTextInputFocus(this);
        base.OnDisappearing();
    }

    private void CollectionView_Scrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        ScrollToTopButtonController.UpdateVisibility(BackToTopButton, e);
    }

    private void BackToTopButton_Clicked(object sender, EventArgs e)
    {
        ScrollToTopButtonController.ScrollToTop(NotesCollectionView, BackToTopButton);
    }
}
