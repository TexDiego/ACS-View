using ACS_View.Domain.Interfaces;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ACS_View.ViewModels;

internal partial class OverallViewModel(IDashboardMetricsService dash) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<Dashboard> dashboard = [];
    [ObservableProperty] private bool isLoading;

    public ICommand GoToPageAsync => new Command<string>(async (p) => await GoToPage(p));
    public ICommand LoadSummaryCommand => new Command(async () => await LoadSummaryAsync());

    private async Task LoadSummaryAsync()
    {
        try
        {
            IsLoading = true;

            Dashboard.Clear();

            var metrics = await dash.GetMetricsAsync();

            Dashboard.Add(new Dashboard()
            {
                Name = "Pacientes",
                Total = metrics.TotalPacientes
            });



        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Erro", "Não foi possível carregar os dados", "Voltar");
            Debug.WriteLine(ex.StackTrace);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task GoToPage(string condition)
    {
        if (condition == "HOUSES")
        {
            await Shell.Current.GoToAsync("//houses");
            return;
        }

        await Shell.Current.GoToAsync("//registers", new Dictionary<string, object> { { "condition", condition } });
    }
}