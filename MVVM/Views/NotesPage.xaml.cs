using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.ViewModel;

namespace ACS_View.MVVM.Views;

public partial class NotesPage : ContentPage
{
	NotesPageViewModel _viewModel;
    DatabaseService _databaseService;
    NoteService _noteService;

    public NotesPage()
    {
        InitializeComponent();
        _databaseService = new DatabaseService();
        _noteService = new NoteService(_databaseService);
        _viewModel = new NotesPageViewModel(_noteService);
        BindingContext = _viewModel; // Configurando aqui
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            _databaseService = new DatabaseService();
            _noteService = new NoteService(_databaseService);
            _viewModel = new NotesPageViewModel(_noteService);
            _viewModel.ScrollToTopRequested += ScrollToTop;

            await _viewModel.LoadNotesAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro de OnAppearing", ex.Message, "Voltar");
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
			await DisplayAlert("Erro", ex.Message, "Voltar");
		}
    }

    private void Btn_SaveNote_Clicked(object sender, EventArgs e)
    {
        NotesContent.Text = string.Empty;
    }
}