using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class NotesPage : ContentPage
{
    private  NotesPageViewModel _viewModel;

    public NotesPage(NotesPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            _viewModel.ScrollToTopRequested += ScrollToTop;

            await _viewModel.LoadNotesAsync();
        }
        catch (Exception ex)
        {
            await this.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }

    private void ScrollToTop()
    {
        NotesCollectionView.ScrollTo(0, position: ScrollToPosition.Start, animate: true);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.ScrollToTopRequested -= ScrollToTop;
    }

    private async void Btn_Back_Clicked(object sender, EventArgs e)
    {
        try
        {
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await this.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }

    private void Btn_SaveNote_Clicked(object sender, EventArgs e)
    {
        NotesContent.Text = string.Empty;
    }
}