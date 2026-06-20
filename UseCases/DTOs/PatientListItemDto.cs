namespace ACS_View.UseCases.DTOs
{
    public sealed class PatientListItemDto
    {
        public int Id { get; set; }
        public string SusNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
