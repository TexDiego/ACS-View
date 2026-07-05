using SQLite;

namespace ACS_View.Domain.Entities;

public class CepAddressCache
{
    [PrimaryKey]
    public string Cep { get; set; } = string.Empty;
    public string Rua { get; set; } = string.Empty;
    public string TipoLogradouro { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Pais { get; set; } = "Brasil";
    public DateTime UpdatedAt { get; set; }
}
