using SQLite;
using System.Text.Json.Serialization;

namespace ACS_View.MVVM.Models
{
    public class House
    {
        [PrimaryKey]
        public int CasaId { get; set; }
        public string CEP { get; set; }

        [JsonPropertyName("logradouro")]
        public string Rua { get; set; }

        public string NumeroCasa { get; set; }

        [JsonPropertyName("bairro")]
        public string Bairro { get; set; }

        [JsonPropertyName("localidade")]
        public string Cidade { get; set; }

        [JsonPropertyName("uf")]
        public string Estado { get; set; }
        public string Pais { get; set; } = "Brasil";
        public string Complemento { get; set; }
        public bool PossuiComplemento { get; set; }
    }
}