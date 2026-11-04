namespace ACS_View.Application.DTOs
{
    public sealed class PatientListItemDto
    {
        public int Id { get; set; }
        public string SusNumber { get; set; } = string.Empty;
        public string PrimarySusNumber { get; set; } = string.Empty;
        public string MotherName { get; set; } = string.Empty;
        public string FatherName { get; set; } = string.Empty;
        public string NisNumber { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
