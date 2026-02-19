using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace ACS_View.MVVM.ViewModels;

internal partial class PersonsInfoViewModel : BaseViewModel
{
    private readonly IPersonsInfoService _infoService = App.ServiceProvider.GetRequiredService<IPersonsInfoService>();
    private readonly string _susNumber;

    public double Width => (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) - 20;

    [ObservableProperty] private ObservableCollection<HealthIcon> icons = [];
    [ObservableProperty] private Patient personInfo;

    [ObservableProperty] private string endereco;
    [ObservableProperty] private string complemento;
    [ObservableProperty] private bool mostrarComplemento;

    public PersonsInfoViewModel(Patient record)
    {
        PersonInfo = record;
        _susNumber = record.SusNumber;

        //MainThread.BeginInvokeOnMainThread(async () => await CarregarEnderecoAsync());
        //SetHealthIcons(record);
    }

    public async Task CarregarEnderecoAsync()
    {
        Endereco = await _infoService.GetEnderecoAsync(_susNumber);
        Complemento = await _infoService.GetComplementoAsync(_susNumber);
        MostrarComplemento = await _infoService.TemComplementoAsync(_susNumber);
    }

    private void SetHealthIcons(Patient record)
    {
        try
        {            
        }
        catch (Exception ex)
        {
            Shell.Current.DisplayAlert("Erro", $"Erro ao carregar ícones de saúde: {ex.Message}", "OK");
        }
    }
}