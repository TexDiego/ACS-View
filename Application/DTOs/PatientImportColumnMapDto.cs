namespace ACS_View.Application.DTOs
{
    public class PatientImportColumnMapDto
    {
        public string NameColumn { get; set; } = "Nome";
        public string SusNumberColumn { get; set; } = "SUS";
        public string MotherNameColumn { get; set; } = "Mãe";
        public string FatherNameColumn { get; set; } = "Pai";
        public string BirthDateColumn { get; set; } = "Data de nascimento";
        public string ObservationColumn { get; set; } = "Observação";
        public string BolsaFamiliaColumn { get; set; } = "Bolsa Família";
        public List<PatientImportConditionColumnDto> HealthConditionColumns { get; set; } = [];
    }
}
