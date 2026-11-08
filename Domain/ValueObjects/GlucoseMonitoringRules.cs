using ACS_View.Domain.Entities;
namespace ACS_View.Domain.ValueObjects;

public static class GlucoseMonitoringRules
{
    public static readonly string[] Methods = ["Não informado", "Glicosímetro", "Sensor contínuo", "Ambos"];
    public static void Validate(PatientGlucoseMonitoring value)
    {
        if (value.ChecksPerDay is <= 0 || value.StripsPerPickup is <= 0 || value.PickupIntervalDays is <= 0)
            throw new ArgumentException("Informe quantidades e intervalos positivos ou deixe os campos vazios.");
        if (!Methods.Contains(value.MonitoringMethod)) throw new ArgumentException("Método de monitoramento inválido.");
        if (value.LastKnownPickup?.Date > DateTime.Today) throw new ArgumentException("A retirada conhecida não pode estar no futuro.");
    }
    public static string Estimate(PatientGlucoseMonitoring? value)
    {
        if (value is null || value.VariableFrequency || value.ChecksPerDay is not > 0)
            return "Sem estimativa: frequência variável ou não informada.";
        var consumption = (long)value.ChecksPerDay.Value * 30;
        var text = $"Consumo estimado em 30 dias: {consumption} fitas.";
        if (value.StripsPerPickup is > 0 && value.PickupIntervalDays is > 0)
        {
            var supply = (decimal)value.StripsPerPickup.Value * 30 / value.PickupIntervalDays.Value;
            text += $" Retirada habitual equivalente: {supply:0.#} fitas/30 dias.";
            if (supply < consumption) text += " Pode ser necessário verificar o acesso aos insumos.";
        }
        return text + " Estimativa informativa baseada na rotina declarada.";
    }
}
