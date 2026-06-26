using ACS_View.Application.Interfaces;

namespace ACS_View.UseCases
{
    public class PatientConditionsSeeder : IPatientConditionSeeder
    {
        public Task SeedAsync()
        {
            return Task.CompletedTask;
        }
    }
}
