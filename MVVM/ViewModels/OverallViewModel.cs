using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels;

internal partial class OverallViewModel : ObservableObject
{
    private readonly IDashboardService _dashboardService = App.ServiceProvider.GetRequiredService<IDashboardService>();

    [ObservableProperty] private ObservableCollection<DashboardItemVM> dashboard = [];

    public ICommand GoToPageAsync => new Command<string>(async (p) => await GoToPage(p));
    public ICommand LoadSummaryCommand => new Command(async () => await LoadSummaryAsync());


    [ObservableProperty] private bool isLoading;

    private async Task LoadSummaryAsync()
    {
        try
        {
            IsLoading = true;

            var items = await _dashboardService.GetDashboardSummaryAsync();

            Dashboard = new ObservableCollection<DashboardItemVM>(
                items.Select(x => new DashboardItemVM
                {
                    Id = x.Id,
                    ItemId = x.ItemId,
                    ItemType = x.ItemType,
                    Name = x.Name,
                    Total = x.Total,
                    DisplayOrder = x.DisplayOrder
                }));
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