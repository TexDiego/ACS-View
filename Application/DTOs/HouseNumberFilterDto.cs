using ACS_View.Domain.ValueObjects;

namespace ACS_View.Application.DTOs;

public sealed record HouseNumberFilterDto(int? Start = null, int? End = null, HouseParity Parity = HouseParity.All)
{
    public bool IsActive => Start.HasValue || End.HasValue || Parity != HouseParity.All;

    public void Validate()
    {
        TerritorialRules.ValidateRange(Start, End);
        if (!Enum.IsDefined(Parity)) throw new ArgumentException("Selecione uma paridade válida.");
    }

    public static HouseNumberFilterDto Parse(string? start, string? end, int parityIndex)
    {
        static int? Bound(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (!int.TryParse(text.Trim(), out var number) || number < 0)
                throw new ArgumentException("Informe números inteiros não negativos nos limites do intervalo.");
            return number;
        }
        var filter = new HouseNumberFilterDto(Bound(start), Bound(end), (HouseParity)parityIndex);
        filter.Validate();
        return filter;
    }

    public string Summary => string.Join(", ", new[]
    {
        (Start, End) switch
        {
            (not null, not null) => $"números de {Start} a {End}",
            (not null, null) => $"números a partir de {Start}",
            (null, not null) => $"números até {End}",
            _ => null
        },
        Parity switch { HouseParity.Even => "pares", HouseParity.Odd => "ímpares", _ => null }
    }.Where(part => part != null));
}
