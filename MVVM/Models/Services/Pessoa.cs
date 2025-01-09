namespace ACS_View.MVVM.Models.Services
{
    public class Pessoa
    {
        public string Nome { get; set; }
        public string Sus { get; set; }
        public DateTime Nascimento { get; set; }
        public string Idade { get; set; }
        public string? Observacao { get; set; }
        public bool HasObs { get; set; }
        public int IdHouse { get; set; }
        public int IdFamily { get; set; }
        public string? Endereco { get; set; }
    }
}
