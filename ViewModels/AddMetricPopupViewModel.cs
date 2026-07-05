using ACS_View.Application.DTOs;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.ViewModels;

public partial class AddMetricPopupViewModel : BaseViewModel
{
    private const int MaxSelectedMetrics = 3;
    private const string AllSexesOption = "Todos";
    private readonly Func<DashboardMetricCreateRequestDto, string?>? validateRequest;

    [ObservableProperty] private ObservableCollection<DashboardMetricOptionDto> metrics = [];
    [ObservableProperty] private string selectedSex = AllSexesOption;
    [ObservableProperty] private string minimumAge = string.Empty;
    [ObservableProperty] private string maximumAge = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string selectedCountText = $"0 de {MaxSelectedMetrics} selecionadas";

    public AddMetricPopupViewModel(
        IEnumerable<Dashboard> candidates,
        Func<DashboardMetricCreateRequestDto, string?>? validateRequest = null)
    {
        this.validateRequest = validateRequest;
        Metrics = new ObservableCollection<DashboardMetricOptionDto>(
            candidates.Select(metric => new DashboardMetricOptionDto { Metric = metric }));

        ToggleMetricCommand = new Command<DashboardMetricOptionDto>(ToggleMetric);
    }

    public IReadOnlyList<string> SexOptions { get; } =
    [
        AllSexesOption,
        nameof(Sexo.Feminino),
        nameof(Sexo.Masculino),
        nameof(Sexo.Indeterminado)
    ];

    public ICommand ToggleMetricCommand { get; }

    public bool TryCreateRequest(out DashboardMetricCreateRequestDto? request, out string errorMessage)
    {
        request = null;
        errorMessage = string.Empty;

        var selectedMetrics = Metrics.Where(metric => metric.IsSelected).ToList();
        if (selectedMetrics.Count == 0)
        {
            errorMessage = "Selecione ao menos uma metrica.";
            SetError(errorMessage);
            return false;
        }

        if (selectedMetrics.Count > MaxSelectedMetrics)
        {
            errorMessage = $"Selecione no maximo {MaxSelectedMetrics} metricas.";
            SetError(errorMessage);
            return false;
        }

        if (!TryParseAge(MinimumAge, "idade minima", out var minimumAge, out errorMessage) ||
            !TryParseAge(MaximumAge, "idade maxima", out var maximumAge, out errorMessage))
        {
            SetError(errorMessage);
            return false;
        }

        if (minimumAge.HasValue && maximumAge.HasValue && minimumAge > maximumAge)
        {
            errorMessage = "A idade minima nao pode ser maior que a idade maxima.";
            SetError(errorMessage);
            return false;
        }

        var hasModifier =
            SelectedSex != AllSexesOption ||
            minimumAge.HasValue ||
            maximumAge.HasValue;

        if (selectedMetrics.Count == 1 && !hasModifier)
        {
            errorMessage = "Adicione ao menos um modificador para criar uma metrica com um unico indicador.";
            SetError(errorMessage);
            return false;
        }

        var filterKeys = selectedMetrics
            .Select(metric => metric.FilterKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        request = new DashboardMetricCreateRequestDto
        {
            FilterKeys = filterKeys,
            FirstFilterKey = filterKeys.ElementAtOrDefault(0) ?? string.Empty,
            SecondFilterKey = filterKeys.ElementAtOrDefault(1) ?? string.Empty,
            SexModifier = SelectedSex == AllSexesOption ? null : SelectedSex,
            MinimumAgeModifier = minimumAge,
            MaximumAgeModifier = maximumAge
        };

        var validationError = validateRequest?.Invoke(request);
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            errorMessage = validationError;
            SetError(errorMessage);
            return false;
        }

        ClearError();
        return true;
    }

    private void ToggleMetric(DashboardMetricOptionDto? option)
    {
        if (option is null)
        {
            return;
        }

        ClearError();

        if (option.IsSelected)
        {
            option.IsSelected = false;
            UpdateSelectedCountText();
            return;
        }

        if (Metrics.Count(metric => metric.IsSelected) >= MaxSelectedMetrics)
        {
            SetError("Remova uma metrica para selecionar outra.");
            return;
        }

        option.IsSelected = true;
        UpdateSelectedCountText();
    }

    private static bool TryParseAge(string value, string fieldName, out int? age, out string errorMessage)
    {
        age = null;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!int.TryParse(value.Trim(), out var parsedAge) || parsedAge < 0 || parsedAge > 120)
        {
            errorMessage = $"Informe uma {fieldName} valida entre 0 e 120.";
            return false;
        }

        age = parsedAge;
        return true;
    }

    private void UpdateSelectedCountText()
    {
        SelectedCountText = $"{Metrics.Count(metric => metric.IsSelected)} de {MaxSelectedMetrics} selecionadas";
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    private void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }
}
