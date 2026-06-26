namespace ACS_View.Application.DTOs;

public class HouseImportColumnMapDto
{
    public string CepColumn { get; set; } = "CEP";
    public string StreetTypeColumn { get; set; } = "Tipo de logradouro";
    public string StreetColumn { get; set; } = "Rua";
    public string NumberColumn { get; set; } = "Numero";
    public string NeighborhoodColumn { get; set; } = "Bairro";
    public string CityColumn { get; set; } = "Cidade";
    public string StateColumn { get; set; } = "Estado";
    public string CountryColumn { get; set; } = "Pais";
    public string ComplementColumn { get; set; } = "Complemento";
}
