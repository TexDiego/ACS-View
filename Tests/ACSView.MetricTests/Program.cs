using ACS_View.Application.DTOs;
using ACS_View.Domain.ValueObjects;
using ACS_View.ViewModels;
using System.Text.Json;

var checks = 0;
void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
    checks++;
}

AddMetricPopupViewModel Create() => new([], request =>
    DashboardMetricCombinationRules.GetValidationError(request.GetSelectedFilterKeys(), false,
        request.SexModifier, request.MinimumAgeModifier, request.MaximumAgeModifier));

foreach (var sex in new[] { "Masculino", "Feminino", "Indeterminado" })
{
    var vm = Create();
    vm.SelectedSex = sex;
    Assert(vm.TryCreateRequest(out var request, out _), $"Sexo {sex} deve dispensar indicador.");
    Assert(request!.GetSelectedFilterKeys().Count == 0 && request.SexModifier == sex, "Filtro pessoal deve preservar sexo sem indicador implícito.");

    var preferences = new DashboardMetricPreferencesDto
    {
        Combinations = [new() { SexModifier = sex, IsHealth = false }]
    };
    var restored = JsonSerializer.Deserialize<DashboardMetricPreferencesDto>(JsonSerializer.Serialize(preferences))!.Combinations.Single();
    Assert(DashboardMetricCombinationRules.CanCombine(restored.GetSelectedFilterKeys(), restored.IsHealth, restored.SexModifier), "Métrica pessoal salva deve continuar válida após desserializar.");
    var key = DashboardFilterKeys.CreateModified(DashboardFilterKeys.All, restored.SexModifier, null, null);
    Assert(DashboardFilterKeys.GetBaseFilterKey(key) == DashboardFilterKeys.All && DashboardFilterKeys.GetModifiers(key).Sex == sex, "Contagem e navegação devem receber ALL com filtro de sexo.");
}

foreach (var (minimum, maximum) in new[] { ("18", "29"), ("60", ""), ("", "5"), ("0", "0") })
{
    var vm = Create();
    vm.MinimumAge = minimum;
    vm.MaximumAge = maximum;
    Assert(vm.TryCreateRequest(out var request, out _), "Faixa etária deve dispensar indicador e aceitar limites individuais e zero.");
    Assert(request!.GetSelectedFilterKeys().Count == 0 && request.SexModifier is null, "Todos os sexos deve manter a faixa etária sem filtro de sexo.");
}

var combined = Create();
combined.SelectedSex = "Feminino";
combined.MinimumAge = "25";
combined.MaximumAge = "64";
Assert(combined.TryCreateRequest(out _, out _), "Sexo e idade devem combinar sem indicador.");
Assert(!Create().TryCreateRequest(out _, out _), "Formulário vazio deve ser rejeitado.");
foreach (var (minimum, maximum) in new[] { ("30", "20"), ("-1", ""), ("", "121"), ("abc", "") })
{
    var vm = Create();
    vm.SelectedSex = "Masculino";
    vm.MinimumAge = minimum;
    vm.MaximumAge = maximum;
    Assert(!vm.TryCreateRequest(out _, out _), "Sexo não deve permitir idade inválida.");
}
Assert(!DashboardMetricCombinationRules.CanCombine([], true, sexModifier: "Feminino"), "Filtro pessoal pertence à aba geral.");
Assert(!DashboardMetricCombinationRules.CanCombine([], false), "Sem indicador nem filtro não deve criar métrica.");
Assert(!DashboardMetricCombinationRules.CanCombine([DashboardFilterKeys.NoHouse], false), "Indicador único sem modificador continua rejeitado.");
Assert(DashboardMetricCombinationRules.CanCombine([DashboardFilterKeys.NoHouse], false, minimumAgeModifier: 18), "Indicador com idade continua aceito.");
Assert(DashboardMetricCombinationRules.CanCombine([DashboardFilterKeys.NoHouse, DashboardFilterKeys.NoFamily], false), "Uniões existentes continuam aceitas.");
Assert(!DashboardMetricCombinationRules.CanCombine([DashboardFilterKeys.Elderly, DashboardFilterKeys.ChildrenUnder6], false), "Indicadores incompatíveis continuam rejeitados.");
Console.WriteLine($"Metric tests passed: {checks} checks.");
