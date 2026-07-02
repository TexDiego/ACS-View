using ACS_View.Domain.ValueObjects;

namespace ACS_View.ViewModels
{
    public class VaccinesInfoViewModel : BaseViewModel
    {
        private const string NotAvailable = "Informação não disponível";

        public string DiseaseDescription { get; }
        public string VaccineName { get; }

        public VaccinesInfoViewModel(string vaccineName)
        {
            var definition = VaccineDoseCatalog.GetDefinition(vaccineName);

            VaccineName = definition?.VaccineName ?? NotAvailable;
            DiseaseDescription = string.IsNullOrWhiteSpace(definition?.DiseaseDescription)
                ? NotAvailable
                : definition.DiseaseDescription;
        }
    }
}
