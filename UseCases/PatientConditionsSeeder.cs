using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.Interfaces;

namespace ACS_View.UseCases
{
    public class PatientConditionsSeeder(ISQLiteConditionsRepository repository) : IPatientConditionSeeder
    {
        public async Task SeedAsync()
        {
            var Conditions = new List<string>
            {
                "Alcoolismo",
                "Tabagismo",
                "Acamado",
                "Domiciliado",
                "Obesidade"
            };

            var patientConditions = Conditions.Select(desc => new PatientConditions
            {
                Description = desc
            }).ToList();

            await repository.InsertConditionsAsync(patientConditions);
        }
    }
}