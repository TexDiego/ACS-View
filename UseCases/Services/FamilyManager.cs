using ACS_View.Application.Interfaces;

namespace ACS_View.UseCases.Services
{
    internal class FamilyManager(IPatientService service) : IFamilyManager
    {
        public async Task AddPeopleToFamily(List<int> ids, int houseId, int familyId)
        {
            foreach (var id in ids)
            {
                var pessoa = await service.GetPatientById(id);

                if (pessoa != null)
                {
                    pessoa.FamilyId = familyId;
                    pessoa.HouseId = houseId;
                    await service.UpdatePatient(pessoa);
                }
            }
        }

        public async Task RemovePersonFromFamily(int id)
        {
            var pessoa = await service.GetPatientById(id);

            if (pessoa != null)
            {
                pessoa.FamilyId = -1;
                pessoa.HouseId = -1;
                await service.UpdatePatient(pessoa);
            }
        }
    }
}
