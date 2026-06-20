namespace ACS_View.UseCases.DTOs
{
    public sealed class HouseListItemDto
    {
        public int CasaId { get; set; }
        public string Rua { get; set; } = string.Empty;
        public string NumeroCasa { get; set; } = string.Empty;
        public string Complemento { get; set; } = string.Empty;
    }
}
