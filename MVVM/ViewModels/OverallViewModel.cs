using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ACS_View.MVVM.ViewModels;

public partial class OverallViewModel : ObservableObject
{
    private readonly IHealthSummaryService _summaryService = App.ServiceProvider.GetRequiredService<IHealthSummaryService>();
    private readonly IDatabaseService _databaseService = App.ServiceProvider.GetRequiredService<IDatabaseService>();

    public OverallViewModel()
    {
        MainThread.BeginInvokeOnMainThread(async () => await _databaseService.InitializeAsync());

        LoadSummaryCommand = new AsyncRelayCommand(LoadSummaryAsync);
        LoadSummaryCommand.Execute(null);

        GoToPageAsync = new Command<string>(async (p) => await GoToPage(p));
    }

    public Command GoToPageAsync { get; }
    public IAsyncRelayCommand LoadSummaryCommand { get; }

    [ObservableProperty] private int totalGestantes;
    [ObservableProperty] private int totalDiabeticos;
    [ObservableProperty] private int totalHipertensos;
    [ObservableProperty] private int totalDiabetesHipertensao;
    [ObservableProperty] private int totalTuberculose;
    [ObservableProperty] private int totalHanseniase;
    [ObservableProperty] private int totalAcamados;
    [ObservableProperty] private int totalDomiciliados;
    [ObservableProperty] private int totalMenores6Anos;
    [ObservableProperty] private int totalMental;
    [ObservableProperty] private int totalFumante;
    [ObservableProperty] private int totalAlcoolatra;
    [ObservableProperty] private int totalDeficiente;
    [ObservableProperty] private int totalHeartDesease;
    [ObservableProperty] private int totalKidneyDesease;
    [ObservableProperty] private int totalLungDesease;
    [ObservableProperty] private int totalLiverDesease;
    [ObservableProperty] private int totalBolsaFamilia;
    [ObservableProperty] private int totalNeurodivergents;
    [ObservableProperty] private int totalDrugsAddicted;
    [ObservableProperty] private int totalHIV;
    [ObservableProperty] private int totalCancer;
    [ObservableProperty] private int totalOld;
    [ObservableProperty] private int total;
    [ObservableProperty] private int totalHouses;
    [ObservableProperty] private int noResidence;
    [ObservableProperty] private bool isLoading;

    private async Task LoadSummaryAsync()
    {
        try
        {
            IsLoading = true;
            var resumo = await _summaryService.GetHealthSummaryAsync();

            TotalHouses = resumo.TotalHouses;
            TotalGestantes = resumo.TotalGestantes;
            TotalDiabeticos = resumo.TotalDiabeticos;
            TotalHipertensos = resumo.TotalHipertensos;
            TotalDiabetesHipertensao = resumo.TotalDiabetesHipertensao;
            TotalTuberculose = resumo.TotalTuberculose;
            TotalHanseniase = resumo.TotalHanseniase;
            TotalAcamados = resumo.TotalAcamados;
            TotalDomiciliados = resumo.TotalDomiciliados;
            TotalMenores6Anos = resumo.TotalMenores6Anos;
            TotalMental = resumo.TotalMental;
            TotalFumante = resumo.TotalFumante;
            TotalAlcoolatra = resumo.TotalAlcoolatra;
            TotalDeficiente = resumo.TotalDeficiente;
            TotalHeartDesease = resumo.TotalHeartDesease;
            TotalKidneyDesease = resumo.TotalKidneyDesease;
            TotalLungDesease = resumo.TotalLungDesease;
            TotalLiverDesease = resumo.TotalLiverDesease;
            TotalBolsaFamilia = resumo.TotalBolsaFamilia;
            TotalNeurodivergents = resumo.TotalNeurodivergents;
            TotalDrugsAddicted = resumo.TotalDrugsAddicted;
            TotalHIV = resumo.TotalHIV;
            TotalCancer = resumo.TotalCancer;
            TotalOld = resumo.TotalOld;
            Total = resumo.Total;
            NoResidence = resumo.NoResidence;
        }
        catch
        {
            await Application.Current.MainPage.DisplayAlert("Erro", "Não foi possível carregar os dados", "Voltar");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static async Task GoToPage(string condition)
    {
        ContentPage page = condition == "HOUSES" ? new HousesPage() : new Registers(condition);
        await App.Current.MainPage.Navigation.PushAsync(page);
    }
}