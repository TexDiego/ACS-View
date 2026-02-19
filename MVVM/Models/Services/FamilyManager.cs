using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Views;

namespace ACS_View.MVVM.Models.Services
{
    internal class FamilyManager : IFamilyManager
    {
        private readonly IPatientService _patientService = App.ServiceProvider.GetRequiredService<IPatientService>();

        public async Task AddPeopleToFamily(List<int> ids, int houseId, int familyId)
        {
            foreach (var id in ids)
            {
                var pessoa = await _patientService.GetPatientById(id);

                if (pessoa != null)
                {
                    pessoa.FamilyId = familyId;
                    pessoa.HouseId = houseId;
                    await _patientService.UpdatePatient(pessoa);
                }
            }
        }

        public async Task RemovePersonFromFamily(int id)
        {
            var pessoa = await _patientService.GetPatientById(id);

            if (pessoa != null)
            {
                pessoa.FamilyId = 0;
                pessoa.HouseId = 0;
                await _patientService.UpdatePatient(pessoa);
            }
        }
    }
}
