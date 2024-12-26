using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;
using System.Text.RegularExpressions;

namespace ACS_View.MVVM.Views;

public partial class AddHouse : ContentPage
{
    private readonly AddHouseViewModel viewModel;

    public AddHouse(DatabaseService databaseService)
    {
        InitializeComponent();
        var houseservice = new HouseService(databaseService);
        viewModel = new AddHouseViewModel(houseservice);

        BindingContext = viewModel;

        viewModel.IsEdit = false;
    }

    public AddHouse(House house, DatabaseService databaseService, int id)
    {
        InitializeComponent();
        var databaseservice = databaseService;
        var houseservice = new HouseService(databaseservice);
        viewModel = new AddHouseViewModel(houseservice);

        BindingContext = viewModel;

        viewModel.HouseId = id;
        viewModel.IsEdit = true;
        Entry_CEP.Text = house.CEP;
        Logradouro.Text = house.Rua;
        Numero.Text = house.NumeroCasa;
        Cidade.Text = house.Cidade;
        Bairro.Text = house.Bairro;
        Estado.Text = house.Estado;
        Complemento.Text = house.Complemento;
       
    }

    private async void Btn_Salvar_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (viewModel == null)
            {
                await this.ShowPopupAsync(new DisplayPopUp("Erro", "ViewModel não foi iniciada corretamente", true, "Voltar", false, ""));
                return;
            }

            viewModel.CEP = Entry_CEP.Text;
            viewModel.Bairro = Bairro.Text;
            viewModel.Numero = Numero.Text;
            viewModel.Rua = Logradouro.Text;
            viewModel.Cidade = Cidade.Text;
            viewModel.Estado = Estado.Text;
            viewModel.Pais = "Brasil";
            viewModel.Complemento = Complemento.Text;
            viewModel.PossuiComplemento = !string.IsNullOrEmpty(Complemento.Text);

            Entry_CEP.Unfocus(); Entry_CEP.Text = string.Empty;
            Logradouro.Unfocus(); Logradouro.Text = string.Empty;
            Numero.Unfocus(); Numero.Text = string.Empty;
            Bairro.Unfocus(); Bairro.Text = string.Empty;
            Estado.Unfocus(); Estado.Text = string.Empty;
            Complemento.Unfocus(); Complemento.Text = string.Empty;
            Cidade.Unfocus(); Cidade.Text = string.Empty;

            if (viewModel.SalvarCommand.CanExecute(null))
            {
                viewModel.SalvarCommand.Execute(null);
            }
        }
        catch (Exception ex)
        {
            await this.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }

    private void Btn_Voltar_Clicked(object sender, EventArgs e)
    {
        Navigation.PopAsync();
    }

    private async void Btn_SearchCEP_Clicked(object sender, EventArgs e)
    {
        string cep = Entry_CEP.Text;

        if (!ValidarCep(cep))
        {
            await this.ShowPopupAsync(new DisplayPopUp("Ops", "CEP inválido", false, "", true, "Voltar"));
            return;
        }

        try
        {
            AI.IsRunning = true;
            AI.IsVisible = true;

            var endereco = await CEPService.BuscarEnderecoPorCep(cep);

            if (endereco != null && !string.IsNullOrEmpty(endereco.Rua))
            {
                Logradouro.Text = endereco.Rua;
                Bairro.Text = endereco.Bairro;
                Cidade.Text = endereco.Cidade;
                Estado.Text = endereco.Estado;

                AI.IsRunning = false;
                AI.IsVisible = false;
            }
            else
            {
                AI.IsRunning = false;
                AI.IsVisible = false;
                await this.ShowPopupAsync(new DisplayPopUp("Erro", "Endereço não encontrado ou CEP inválido.", false, "", true, "OK"));
            }
        }
        catch (HttpRequestException ex)
        {
            await this.ShowPopupAsync(new DisplayPopUp("Erro", $"Não foi possível conectar ao serviço de CEP.\nVerifique sua conexão com a internet.\n\n {ex.Message}", false, "", true, "OK"));
        }
        catch (Exception ex)
        {
            await this.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }

    private bool ValidarCep(string cep)
    {
        return !string.IsNullOrWhiteSpace(cep) && cep.Length == 8 && cep.All(char.IsDigit);
    }

    private void Entry_CEP_TextChanged(object sender, TextChangedEventArgs e)
    {
        var regex = new Regex("^[0-9]*$");

        if (!string.IsNullOrEmpty(e.NewTextValue) && !regex.IsMatch(e.NewTextValue))
        {
            // Reverte para o texto anterior caso contenha caracteres inválidos
            ((Entry)sender).Text = e.OldTextValue;
        }
    }
}