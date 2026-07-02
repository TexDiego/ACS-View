namespace ACS_View.Domain.ValueObjects;

public static class StreetAddressParser
{
    private static readonly IReadOnlyList<(string Canonical, string[] Aliases)> StreetTypes =
    [
        ("Acesso Local", ["Acesso Local"]),
        ("Área Especial", ["Área Especial", "Area Especial"]),
        ("Complexo Viário", ["Complexo Viário", "Complexo Viario"]),
        ("Distrito Industrial", ["Distrito Industrial"]),
        ("Entre Quadra", ["Entre Quadra"]),
        ("Núcleo Rural", ["Núcleo Rural", "Nucleo Rural"]),
        ("Aeroporto", ["Aeroporto"]),
        ("Alameda", ["Alameda"]),
        ("Avenida", ["Avenida", "Av", "Av."]),
        ("Balneário", ["Balneário", "Balneario"]),
        ("Belvedere", ["Belvedere"]),
        ("Boulevard", ["Boulevard"]),
        ("Caminho", ["Caminho"]),
        ("Chácara", ["Chácara", "Chacara"]),
        ("Colônia", ["Colônia", "Colonia"]),
        ("Comunidade", ["Comunidade"]),
        ("Condomínio", ["Condomínio", "Condominio"]),
        ("Conjunto", ["Conjunto"]),
        ("Distrito", ["Distrito"]),
        ("Esplanada", ["Esplanada"]),
        ("Estação", ["Estação", "Estacao"]),
        ("Estrada", ["Estrada"]),
        ("Fazenda", ["Fazenda"]),
        ("Jardim", ["Jardim"]),
        ("Ladeira", ["Ladeira"]),
        ("Loteamento", ["Loteamento"]),
        ("Núcleo", ["Núcleo", "Nucleo"]),
        ("Parque", ["Parque"]),
        ("Passagem", ["Passagem"]),
        ("Passarela", ["Passarela"]),
        ("Praça", ["Praça", "Praca"]),
        ("Quadra", ["Quadra"]),
        ("Recanto", ["Recanto"]),
        ("Residencial", ["Residencial"]),
        ("Rodovia", ["Rodovia"]),
        ("Setor", ["Setor"]),
        ("Travessa", ["Travessa", "Tv", "Tv."]),
        ("Trecho", ["Trecho"]),
        ("Trevo", ["Trevo"]),
        ("Viaduto", ["Viaduto"]),
        ("Viela", ["Viela"]),
        ("Acesso", ["Acesso"]),
        ("Adro", ["Adro"]),
        ("Alto", ["Alto"]),
        ("Área", ["Área", "Area"]),
        ("Beco", ["Beco"]),
        ("Bloco", ["Bloco"]),
        ("Bosque", ["Bosque"]),
        ("Campo", ["Campo"]),
        ("Eixo", ["Eixo"]),
        ("Favela", ["Favela"]),
        ("Feira", ["Feira"]),
        ("Lago", ["Lago"]),
        ("Lagoa", ["Lagoa"]),
        ("Largo", ["Largo"]),
        ("Morro", ["Morro"]),
        ("Pátio", ["Pátio", "Patio"]),
        ("Rua", ["Rua", "R", "R."]),
        ("Sítio", ["Sítio", "Sitio"]),
        ("Vale", ["Vale"]),
        ("Vereda", ["Vereda"]),
        ("Via", ["Via"]),
        ("Vila", ["Vila"])
    ];

    public static StreetAddressParts SplitStreetType(string? fullStreetName)
    {
        var normalizedStreet = NormalizeWhitespace(fullStreetName);
        if (string.IsNullOrWhiteSpace(normalizedStreet))
        {
            return new StreetAddressParts(string.Empty, string.Empty);
        }

        foreach (var (canonical, aliases) in StreetTypes)
        {
            foreach (var alias in aliases)
            {
                var prefixLength = GetPrefixLength(normalizedStreet, alias);
                if (prefixLength <= 0)
                {
                    continue;
                }

                var streetName = normalizedStreet[prefixLength..].Trim();
                return string.IsNullOrWhiteSpace(streetName)
                    ? new StreetAddressParts(string.Empty, normalizedStreet)
                    : new StreetAddressParts(canonical, streetName);
            }
        }

        return new StreetAddressParts(string.Empty, normalizedStreet);
    }

    private static int GetPrefixLength(string value, string prefix)
    {
        if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (value.Length == prefix.Length)
        {
            return 0;
        }

        return char.IsWhiteSpace(value[prefix.Length])
            ? prefix.Length
            : 0;
    }

    private static string NormalizeWhitespace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(" ", value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}

public readonly record struct StreetAddressParts(string StreetType, string StreetName);
