using System.Globalization;
using System.Text;

namespace ACS_View.Domain.ValueObjects;

public static class ImportColumnAliases
{
    private static readonly string[][] Groups =
    [
        ["Nome", "cadastro nome", "usuario saude nome"],
        ["SUS", "CNS", "cadastro cns"],
        ["Mae", "nome da mae"], ["Pai", "nome do pai"],
        ["Data de nascimento", "cadastro dn", "data nascimento"],
        ["Sexo", "cadastro sexo"], ["Observacao", "outra condicao de saude"],
        ["Gestante", "esta gestante"], ["Diabetes"], ["Hipertensão"],
        ["Tuberculose"], ["Hanseníase"], ["Acamado", "esta acamado"],
        ["Domiciliado", "esta domiciliado"],
        ["Condição mental", "diagnostico problema de saude"],
        ["Fumante"], ["Alcoólatra", "usa alcool"],
        ["Portador de câncer", "cancer"], ["Dependente químico", "usa drogas"],
        ["Responsavel familiar", "resp. familiar?"],
        ["SUS do responsavel", "resp. familiar cns", "cns do responsavel familiar"],
        ["Numero", "numero imovel"], ["Rua", "logradouro"],
        ["Tipo de logradouro", "tipo logradouro"]
    ];

    public static string Normalize(string value) => new(value.Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
        .Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    public static IReadOnlyList<string> For(string column)
    {
        var key = Normalize(column);
        var group = Groups.FirstOrDefault(g => g.Any(a => Normalize(a) == key));
        return new[] { column }.Concat(group ?? []).Distinct().ToArray();
    }

    public static bool Matches(string header, string column) =>
        For(column).Any(alias => Normalize(alias) == Normalize(header));
}
