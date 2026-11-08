using ACS_View.Domain.Entities;
namespace ACS_View.Domain.ValueObjects;

public static class InfectiousFollowUpRules
{
    public const string Tuberculosis = "tuberculosis";
    public const string Hiv = "hiv";
    public const string Immunodeficiency = "immunodeficiency";
    public static string[] Statuses(string code) => code switch
    {
        Tuberculosis => ["Não informado", "Em acompanhamento", "Em tratamento", "Tratamento concluído", "Acompanhamento interrompido"],
        Hiv or Immunodeficiency => ["Não informado", "Em acompanhamento", "Em tratamento", "Acompanhamento interrompido"],
        _ => throw new ArgumentException("Condição de acompanhamento inválida.")
    };
    public static void Validate(PatientInfectiousFollowUp value)
    {
        if(!Statuses(value.ConditionCode).Contains(value.Status)) throw new ArgumentException("Situação de acompanhamento inválida.");
        if(value.TreatmentStarted?.Date > DateTime.Today || value.LastAppointment?.Date > DateTime.Today)
            throw new ArgumentException("Início de tratamento e última consulta não podem estar no futuro.");
        if(value.TreatmentStarted is DateTime start && value.NextAppointment is DateTime next && next.Date < start.Date)
            throw new ArgumentException("A próxima consulta não pode anteceder o início informado do tratamento.");
    }
}
