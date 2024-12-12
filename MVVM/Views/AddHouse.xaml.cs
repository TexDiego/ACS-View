using ACS_View.MVVM.Models.Services;
using CommunityToolkit.Maui.Views;
using System.Text.RegularExpressions;

namespace ACS_View.MVVM.Views;

public partial class AddHouse : ContentPage
{
	public AddHouse()
	{
		InitializeComponent();
	}

    private void Btn_Salvar_Clicked(object sender, EventArgs e)
    {

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
                await DisplayAlert("Erro", "Endereço não encontrado ou CEP inválido.", "OK");
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