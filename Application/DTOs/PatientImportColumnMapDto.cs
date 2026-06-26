namespace ACS_View.Application.DTOs
{
    public class PatientImportColumnMapDto
    {
        public string NameColumn { get; set; } = "Nome";
        public string SusNumberColumn { get; set; } = "SUS";
        public string MotherNameColumn { get; set; } = "Mae";
        public string FatherNameColumn { get; set; } = "Pai";
        public string SexColumn { get; set; } = "Sexo";
        public string BirthDateColumn { get; set; } = "Data de nascimento";
        public string ObservationColumn { get; set; } = "Observacao";
        public string BolsaFamiliaColumn { get; set; } = "Bolsa Familia";
        public string PatientCepColumn { get; set; } = "CEP";
        public string PatientStreetTypeColumn { get; set; } = "Tipo de logradouro";
        public string PatientStreetColumn { get; set; } = "Rua";
        public string PatientHouseNumberColumn { get; set; } = "Numero";
        public string PatientNeighborhoodColumn { get; set; } = "Bairro";
        public string PatientCityColumn { get; set; } = "Cidade";
        public string PatientStateColumn { get; set; } = "Estado";
        public string PatientComplementColumn { get; set; } = "Complemento";
        public string IsFamilyResponsibleColumn { get; set; } = "Responsavel familiar";
        public string FamilyResponsibleSusColumn { get; set; } = "SUS do responsavel";
        public bool EnableAutomaticFamilyLinking { get; set; } = true;
        public bool OverwriteExistingFamilyLinks { get; set; } = false;
        public bool AllowGlobalUniqueParentMatch { get; set; } = true;
        public bool AllowGlobalUniqueResponsibleMatch { get; set; } = false;
        public int MinimumParentAgeDifferenceYears { get; set; } = 12;
        public int MaximumMotherAgeDifferenceYears { get; set; } = 60;
        public int MaximumFatherAgeDifferenceYears { get; set; } = 80;
        public List<PatientImportConditionColumnDto> HealthConditionColumns { get; set; } = [];
    }
}
