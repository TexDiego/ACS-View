using ACS_View.Domain.Entities;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;

namespace ACS_View.Application.DTOs;

public sealed class PregnancyDetailsDto
{
    public PatientPregnancy Pregnancy { get; set; } = new();
    public int RegisteredChildrenCount { get; set; }
    public PregnancyRiskSuggestion RiskSuggestion { get; set; } = new();
    public GestationalAge? GestationalAge { get; set; }
    public int? Trimester { get; set; }
    public bool IsPuerperal { get; set; }
    public int? PostpartumDays { get; set; }
    public DateTime? PuerperiumEndDate { get; set; }

    public string RiskText => Pregnancy.ManualRisk switch
    {
        PregnancyRisk.NotInformed => "Não informado",
        PregnancyRisk.Usual => "Habitual",
        PregnancyRisk.Attention => "Atenção",
        PregnancyRisk.HighRisk => "Alto risco",
        _ => Pregnancy.ManualRisk.ToString()
    };

    public string SuggestedRiskText => RiskSuggestion.Risk switch
    {
        PregnancyRisk.NotInformed => "Não informado",
        PregnancyRisk.Usual => "Habitual",
        PregnancyRisk.Attention => "Atenção",
        PregnancyRisk.HighRisk => "Alto risco",
        _ => RiskSuggestion.Risk.ToString()
    };
}
