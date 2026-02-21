using SQLite;
using System.Text.Json.Serialization;

namespace ACS_View.MVVM.Models
{
    public class House
    {
        [PrimaryKey, AutoIncrement]
        public int CasaId { get; set; }

        [JsonPropertyName("cep")]
        public string CEP { get; set; } = string.Empty;

        [JsonPropertyName("logradouro")]
        public string Rua { get; set; } = string.Empty;

        public string NumeroCasa { get; set; } = string.Empty;

        [JsonPropertyName("bairro")]
        public string Bairro { get; set; } = string.Empty;

        [JsonPropertyName("localidade")]
        public string Cidade { get; set; } = string.Empty;

        [JsonPropertyName("uf")]
        public string Estado { get; set; } = string.Empty;
        public string Pais { get; set; } = "Brasil";
        public string Complemento { get; set; } = string.Empty;
        public bool PossuiComplemento { get; set; } = false;
    }
}