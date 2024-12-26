namespace ACS_View.MVVM.Models.Services
{
    public class Casa
    {
        public int Id { get; set; }
        public string Rua { get; set; }
        public string? Numero { get; set; }
        public string? Complemento { get; set; }
        public bool PossuiComplemento { get; set; }
    }
}
