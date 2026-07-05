using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Diagnostics;
using System.Windows.Input;

namespace ACS_View.ViewModels;

public partial class VisitRecordsViewModel : BaseViewModel
{
    private readonly IVisitsService visitsService;
    private readonly DateTime currentMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private readonly DateTime minimumMonth = new(DateTime.Today.AddYears(-1).Year, DateTime.Today.AddYears(-1).Month, 1);
    private int loadedVersion = -1;
    private bool hasLoaded;
    private VisitStatus? selectedStatus;
    private DateTime selectedMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    [ObservableProperty] private ObservableCollection<VisitRecordFamilyGroupDto> visitGroups = [];
    [ObservableProperty] private ObservableCollection<VisitStatusFilterOption> statusFilters = [];
    [ObservableProperty] private string footerText = string.Empty;
    [ObservableProperty] private string monthTitle = string.Empty;
    [ObservableProperty] private bool canGoPreviousMonth;
    [ObservableProperty] private bool canGoNextMonth;

    public ICommand SelectStatusCommand { get; }
    public ICommand DeleteVisitCommand { get; }
    public ICommand GoToFamilyCommand { get; }
    public ICommand PreviousMonthCommand { get; }
    public ICommand NextMonthCommand { get; }

    public VisitRecordsViewModel(IVisitsService visitsService)
    {
        this.visitsService = visitsService;
        StatusFilters = new ObservableCollection<VisitStatusFilterOption>(
        [
            new("Todos", null, true),
            new("Realizadas", VisitStatus.Completed, false),
            new("Ausentes", VisitStatus.Absent, false),
            new("Recusadas", VisitStatus.Refused, false)
        ]);

        SelectStatusCommand = new Command<VisitStatusFilterOption>(async option => await SelectStatusAsync(option));
        DeleteVisitCommand = new Command<int>(async id => await DeleteVisitAsync(id));
        GoToFamilyCommand = new Command<VisitRecordFamilyGroupDto>(async group => await GoToFamilyAsync(group));
        PreviousMonthCommand = new Command(async () => await ChangeMonthAsync(-1));
        NextMonthCommand = new Command(async () => await ChangeMonthAsync(1));
        UpdateMonthState();
    }

    internal async Task LoadAsync(bool force = false)
    {
        var currentVersion = DataChangeTracker.VisitsVersion + DataChangeTracker.PatientsVersion + DataChangeTracker.HousesVersion;
        if (!force && hasLoaded && loadedVersion == currentVersion)
        {
            return;
        }

        try
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                await visitsService.PurgeExpiredVisitsAsync(DateTime.Today);
                var groups = await visitsService.GetVisitRecordGroupsAsync(selectedMonth, selectedStatus);
                VisitGroups = new ObservableCollection<VisitRecordFamilyGroupDto>(groups);
                var total = groups.Sum(group => group.Visits.Count);
                FooterText = BuildFooterText(total);
                UpdateMonthState();
                loadedVersion = currentVersion;
                hasLoaded = true;
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao carregar registros de visitas: {ex.Message}");
            await DisplayAlertAsync("Erro", "Nao foi possivel carregar os registros de visitas.");
        }
    }

    private async Task SelectStatusAsync(VisitStatusFilterOption? option)
    {
        if (option is null)
        {
            return;
        }

        selectedStatus = option.Status;
        foreach (var filter in StatusFilters)
        {
            filter.IsSelected = ReferenceEquals(filter, option);
        }

        await LoadAsync(force: true);
    }

    private async Task ChangeMonthAsync(int offset)
    {
        var targetMonth = selectedMonth.AddMonths(offset);
        if (targetMonth < minimumMonth || targetMonth > currentMonth)
        {
            return;
        }

        selectedMonth = targetMonth;
        UpdateMonthState();
        await LoadAsync(force: true);
    }

    private async Task DeleteVisitAsync(int id)
    {
        if (id <= 0)
        {
            return;
        }

        var confirm = await DisplayConfirmationAsync("Excluir visita", "Deseja excluir este registro de visita?", "Excluir");
        if (!confirm)
        {
            return;
        }

        await visitsService.DeleteVisitAsync(id);
        await LoadAsync(force: true);
    }

    private async Task GoToFamilyAsync(VisitRecordFamilyGroupDto? group)
    {
        if (group is null || group.HouseId <= 0)
        {
            return;
        }

        await NavigateToAsync("families", new Dictionary<string, object> { { "id", group.HouseId } });
    }

    private string BuildFooterText(int total)
    {
        return selectedStatus switch
        {
            VisitStatus.Completed => total == 1 ? "1 visita realizada" : $"{total} visitas realizadas",
            VisitStatus.Absent => total == 1 ? "1 ausente encontrado" : $"{total} ausentes encontrados",
            VisitStatus.Refused => total == 1 ? "1 recusa encontrada" : $"{total} recusas encontradas",
            _ => total == 1 ? "1 registro encontrado" : $"{total} registros encontrados"
        };
    }

    private void UpdateMonthState()
    {
        var culture = new CultureInfo("pt-BR");
        var title = selectedMonth.ToString("MMMM yyyy", culture);
        MonthTitle = char.ToUpper(title[0], culture) + title[1..];
        CanGoPreviousMonth = selectedMonth > minimumMonth;
        CanGoNextMonth = selectedMonth < currentMonth;
    }
}

public partial class VisitStatusFilterOption : ObservableObject
{
    [ObservableProperty] private bool isSelected;

    public VisitStatusFilterOption(string label, VisitStatus? status, bool isSelected)
    {
        Label = label;
        Status = status;
        this.isSelected = isSelected;
    }

    public string Label { get; }
    public VisitStatus? Status { get; }
}
