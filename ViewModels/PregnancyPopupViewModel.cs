using ACS_View.Application.DTOs;
using ACS_View.Domain.Entities;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.ViewModels;

public partial class PregnancyPopupViewModel : BaseViewModel
{
    private readonly PatientPregnancy pregnancy;
    private readonly Patient patient;
    private readonly int registeredChildrenCount;
    private readonly IReadOnlyList<string> conditionDescriptions;
    private PregnancyRiskSuggestion riskSuggestion = new();

    [ObservableProperty] private bool hasLastMenstrualPeriod;
    [ObservableProperty] private DateTime lastMenstrualPeriodDate = DateTime.Today;
    [ObservableProperty] private bool hasExpectedBirthDate;
    [ObservableProperty] private DateTime expectedBirthDate = DateTime.Today.AddDays(280);
    [ObservableProperty] private string gestationalAgeText = "Sem DUM";
    [ObservableProperty] private string trimesterText = "Trimestre não calculado";
    [ObservableProperty] private string informedChildrenCountText = string.Empty;
    [ObservableProperty] private string selectedRisk = "Não informado";
    [ObservableProperty] private string selectedStatus = "Ativa";
    [ObservableProperty] private bool showEndFields;
    [ObservableProperty] private DateTime endedAt = DateTime.Today;
    [ObservableProperty] private string selectedEndType = "Parto";
    [ObservableProperty] private string notes = string.Empty;
    [ObservableProperty] private string suggestedRiskText = "Habitual";
    [ObservableProperty] private string suggestionReasonsText = string.Empty;
    [ObservableProperty] private bool showRiskSuggestion;

    public PregnancyPopupViewModel(PregnancyDetailsDto details)
    {
        pregnancy = Clone(details.Pregnancy);
        patient = details.Patient;
        registeredChildrenCount = details.RegisteredChildrenCount;
        conditionDescriptions = details.ConditionDescriptions;
        riskSuggestion = details.RiskSuggestion;
        RegisteredChildrenSuggestionText = $"Sugestão: {registeredChildrenCount} filhos cadastrados no app";

        HasLastMenstrualPeriod = pregnancy.LastMenstrualPeriod is not null;
        LastMenstrualPeriodDate = pregnancy.LastMenstrualPeriod?.Date ?? DateTime.Today;
        HasExpectedBirthDate = pregnancy.ExpectedBirthDate is not null;
        ExpectedBirthDate = pregnancy.ExpectedBirthDate?.Date ??
            (pregnancy.LastMenstrualPeriod is null
                ? DateTime.Today.AddDays(280)
                : PregnancyCalculator.CalculateExpectedBirthDate(pregnancy.LastMenstrualPeriod.Value));
        InformedChildrenCountText = pregnancy.InformedChildrenCount?.ToString() ?? string.Empty;
        SelectedRisk = GetRiskText(pregnancy.ManualRisk);
        SelectedStatus = GetStatusText(pregnancy.Status);
        ShowEndFields = pregnancy.Status == PregnancyStatus.Ended;
        EndedAt = pregnancy.EndedAt?.Date ?? DateTime.Today;
        SelectedEndType = GetEndTypeText(pregnancy.EndType ?? PregnancyEndType.Birth);
        Notes = pregnancy.Notes ?? string.Empty;

        UseChildrenSuggestionCommand = new Command(UseChildrenSuggestion);
        ApplyRiskSuggestionCommand = new Command(ApplyRiskSuggestion);
        MarkEndedCommand = new Command(() => SelectedStatus = GetStatusText(PregnancyStatus.Ended));
        Recalculate();
    }

    public ObservableCollection<string> RiskOptions { get; } =
    [
        "Não informado",
        "Habitual",
        "Atenção",
        "Alto risco"
    ];

    public ObservableCollection<string> StatusOptions { get; } =
    [
        "Ativa",
        "Encerrada",
        "Não confirmada"
    ];

    public ObservableCollection<string> EndTypeOptions { get; } =
    [
        "Parto",
        "Aborto",
        "Perda gestacional",
        "Mudança de território",
        "Outro"
    ];

    public string RegisteredChildrenSuggestionText { get; }
    public double BottomSheetWidth => GetBottomSheetSize().Width;
    public double BottomSheetHeight => GetBottomSheetSize().Height;
    public ICommand UseChildrenSuggestionCommand { get; }
    public ICommand ApplyRiskSuggestionCommand { get; }
    public ICommand MarkEndedCommand { get; }

    public PatientPregnancy ToPregnancy()
    {
        pregnancy.LastMenstrualPeriod = HasLastMenstrualPeriod ? LastMenstrualPeriodDate.Date : null;
        pregnancy.ExpectedBirthDate = HasExpectedBirthDate ? ExpectedBirthDate.Date : null;
        pregnancy.ManualRisk = ParseRisk(SelectedRisk);
        pregnancy.Status = ParseStatus(SelectedStatus);
        pregnancy.InformedChildrenCount = int.TryParse(InformedChildrenCountText, out var childrenCount)
            ? Math.Max(0, childrenCount)
            : null;
        pregnancy.EndedAt = pregnancy.Status == PregnancyStatus.Ended ? EndedAt.Date : null;
        pregnancy.EndType = pregnancy.Status == PregnancyStatus.Ended ? ParseEndType(SelectedEndType) : null;
        pregnancy.Notes = Notes ?? string.Empty;
        return pregnancy;
    }

    partial void OnHasLastMenstrualPeriodChanged(bool value)
    {
        if (value)
        {
            HasExpectedBirthDate = true;
            ExpectedBirthDate = PregnancyCalculator.CalculateExpectedBirthDate(LastMenstrualPeriodDate);
        }

        Recalculate();
    }

    partial void OnLastMenstrualPeriodDateChanged(DateTime value)
    {
        if (HasLastMenstrualPeriod)
        {
            HasExpectedBirthDate = true;
            ExpectedBirthDate = PregnancyCalculator.CalculateExpectedBirthDate(value);
        }

        Recalculate();
    }

    partial void OnExpectedBirthDateChanged(DateTime value)
    {
        Recalculate();
    }

    partial void OnHasExpectedBirthDateChanged(bool value)
    {
        Recalculate();
    }

    partial void OnSelectedRiskChanged(string value)
    {
        Recalculate();
    }

    partial void OnSelectedStatusChanged(string value)
    {
        ShowEndFields = ParseStatus(value) == PregnancyStatus.Ended;
        Recalculate();
    }

    partial void OnInformedChildrenCountTextChanged(string value)
    {
        Recalculate();
    }

    private void UseChildrenSuggestion()
    {
        InformedChildrenCountText = registeredChildrenCount.ToString();
        Recalculate();
    }

    private void ApplyRiskSuggestion()
    {
        SelectedRisk = GetRiskText(riskSuggestion.Risk);
    }

    private void Recalculate()
    {
        var preview = ToPreviewPregnancy();
        var age = PregnancyCalculator.CalculateGestationalAge(preview);
        var trimester = PregnancyCalculator.CalculateTrimester(preview);
        riskSuggestion = PregnancyRiskSuggestionCalculator.Calculate(
            patient,
            preview,
            conditionDescriptions,
            registeredChildrenCount);
        GestationalAgeText = age?.ToString() ?? "Sem DUM";
        TrimesterText = trimester is null ? "Trimestre não calculado" : $"{trimester}º trimestre";
        SuggestedRiskText = GetRiskText(riskSuggestion.Risk);
        SuggestionReasonsText = riskSuggestion.ReasonsText;
        ShowRiskSuggestion = riskSuggestion.Risk != ParseRisk(SelectedRisk);
    }

    private PatientPregnancy ToPreviewPregnancy()
    {
        return new PatientPregnancy
        {
            LastMenstrualPeriod = HasLastMenstrualPeriod ? LastMenstrualPeriodDate.Date : null,
            ExpectedBirthDate = HasExpectedBirthDate ? ExpectedBirthDate.Date : null,
            InformedChildrenCount = int.TryParse(InformedChildrenCountText, out var childrenCount)
                ? Math.Max(0, childrenCount)
                : null,
            Status = ParseStatus(SelectedStatus),
            ManualRisk = ParseRisk(SelectedRisk)
        };
    }

    private static PatientPregnancy Clone(PatientPregnancy source)
    {
        return new PatientPregnancy
        {
            Id = source.Id,
            UserId = source.UserId,
            PatientId = source.PatientId,
            LastMenstrualPeriod = source.LastMenstrualPeriod,
            ExpectedBirthDate = source.ExpectedBirthDate,
            Status = source.Status,
            ManualRisk = source.ManualRisk,
            InformedChildrenCount = source.InformedChildrenCount,
            CreatedAt = source.CreatedAt,
            EndedAt = source.EndedAt,
            EndType = source.EndType,
            Notes = source.Notes ?? string.Empty
        };
    }

    private static string GetRiskText(PregnancyRisk risk)
    {
        return risk switch
        {
            PregnancyRisk.NotInformed => "Não informado",
            PregnancyRisk.Usual => "Habitual",
            PregnancyRisk.Attention => "Atenção",
            PregnancyRisk.HighRisk => "Alto risco",
            _ => "Não informado"
        };
    }

    private static PregnancyRisk ParseRisk(string? value)
    {
        return value switch
        {
            "Habitual" => PregnancyRisk.Usual,
            "Atenção" => PregnancyRisk.Attention,
            "Alto risco" => PregnancyRisk.HighRisk,
            _ => PregnancyRisk.NotInformed
        };
    }

    private static string GetStatusText(PregnancyStatus status)
    {
        return status switch
        {
            PregnancyStatus.Active => "Ativa",
            PregnancyStatus.Ended => "Encerrada",
            PregnancyStatus.NotConfirmed => "Não confirmada",
            _ => "Ativa"
        };
    }

    private static PregnancyStatus ParseStatus(string? value)
    {
        return value switch
        {
            "Encerrada" => PregnancyStatus.Ended,
            "Não confirmada" => PregnancyStatus.NotConfirmed,
            _ => PregnancyStatus.Active
        };
    }

    private static string GetEndTypeText(PregnancyEndType endType)
    {
        return endType switch
        {
            PregnancyEndType.Birth => "Parto",
            PregnancyEndType.Abortion => "Aborto",
            PregnancyEndType.PregnancyLoss => "Perda gestacional",
            PregnancyEndType.MovedOut => "Mudança de território",
            PregnancyEndType.Other => "Outro",
            _ => "Parto"
        };
    }

    private static PregnancyEndType ParseEndType(string? value)
    {
        return value switch
        {
            "Aborto" => PregnancyEndType.Abortion,
            "Perda gestacional" => PregnancyEndType.PregnancyLoss,
            "Mudança de território" => PregnancyEndType.MovedOut,
            "Outro" => PregnancyEndType.Other,
            _ => PregnancyEndType.Birth
        };
    }

    private static (double Width, double Height) GetBottomSheetSize()
    {
        try
        {
            var info = DeviceDisplay.MainDisplayInfo;
            var density = info.Density <= 0 ? 1 : info.Density;
            var width = Math.Max(320, info.Width / density);
            var height = Math.Max(520, info.Height / density);
            return (width, height);
        }
        catch
        {
            return (BaseViewModel.DefaultPopupWidth, 720);
        }
    }
}
