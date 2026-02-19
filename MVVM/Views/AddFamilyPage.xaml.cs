using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class AddFamilyPage : ContentPage
{
    private readonly AddFamilyViewModel viewModel;
    private CancellationTokenSource _throttleCts = new();

    public AddFamilyPage(int idHouse, bool isEdit, int? idFamily = null)
    {
        InitializeComponent();
        BindingContext = viewModel = new AddFamilyViewModel(idHouse, isEdit, idFamily);
	}

    private async void Entry_Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        _throttleCts?.Cancel();
        _throttleCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(300, _throttleCts.Token);
            _throttleCts.Token.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(e.NewTextValue))
            {
                Scroll_View_Search.IsVisible = false;
                if (viewModel.SearchCommand.CanExecute(null))
                    viewModel.SearchCommand.Execute(null);

                return;
            }

            if (viewModel.SearchCommand.CanExecute(null))
                viewModel.SearchCommand.Execute(e.NewTextValue);
            Scroll_View_Search.IsVisible = true;
        }
        catch (TaskCanceledException)
        {
            // Ignore exception, tarefa foi cancelada
        }
        catch (Exception ex)
        {
            await Shell.Current.ShowPopupAsync(
                    new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }

    private void ImageButton_Clicked(object sender, EventArgs e)
    {
        Entry_Search.Text = string.Empty;
    }
}