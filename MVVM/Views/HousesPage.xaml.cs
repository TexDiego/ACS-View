using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class HousesPage : ContentPage
{
    private readonly HousesPageViewModel _viewModel;

    public HousesPage(DatabaseService databaseService, HouseService houseService)
    {
        InitializeComponent();

        try
        {
            // Inicializa o ViewModel e define como contexto de Binding
            _viewModel = new HousesPageViewModel(databaseService, houseService);
            BindingContext = _viewModel;
        }
        catch (Exception ex)
        {
            this.ShowPopup(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Carrega os dados na primeira exibição da página
            if (!_viewModel.Houses.Any())
            {
                _viewModel.LoadHousesCommand.Execute(null);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro no OnAppearing: {ex.Message}");
            this.ShowPopup(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        try
        {
            // Recarrega os dados ao retornar para esta página
            _viewModel.LoadHousesCommand.Execute(null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro no OnNavigatedTo: {ex.Message}");
            this.ShowPopup(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }

    private void Btn_Voltar_Clicked(object sender, EventArgs e)
    {
        // Voltar para a página anterior
        Navigation.PopAsync();
    }

    private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Executa o comando de pesquisa com o novo texto
        _viewModel.LoadHousesCommand.Execute(e.NewTextValue);
    }
}
