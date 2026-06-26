using ACS_View.Application.Interfaces;

namespace ACS_View.UseCases.Services
{
    internal class FamilyManager(IPatientService service) : IFamilyManager
    {
        public async Task AddPeopleToFamily(List<int> ids, int houseId, int familyId, int responsiblePatientId)
        {
            if (ids.Count == 0)
            {
                return;
            }

            if (!ids.Contains(responsiblePatientId))
            {
                throw new InvalidOperationException("O responsavel familiar deve fazer parte da familia.");
            }

            var responsible = await service.GetPatientById(responsiblePatientId);
            if (responsible == null || string.IsNullOrWhiteSpace(responsible.SusNumber))
            {
                throw new InvalidOperationException("Responsavel familiar invalido.");
            }

            foreach (var id in ids)
            {
                var pessoa = await service.GetPatientById(id);

                if (pessoa != null)
                {
                    pessoa.FamilyId = familyId;
                    pessoa.HouseId = houseId;
                    pessoa.FamilyResponsibleSus = responsible.SusNumber;
                    await service.UpdatePatient(pessoa);
                }
            }
        }

        public async Task RemovePersonFromFamily(int id)
        {
            var pessoa = await service.GetPatientById(id);

            if (pessoa != null)
            {
                var oldHouseId = pessoa.HouseId;
                var oldFamilyId = pessoa.FamilyId;
                var wasResponsible = !string.IsNullOrWhiteSpace(pessoa.SusNumber) &&
                    string.Equals(pessoa.FamilyResponsibleSus, pessoa.SusNumber, StringComparison.OrdinalIgnoreCase);

                pessoa.FamilyId = -1;
                pessoa.HouseId = -1;
                pessoa.FamilyResponsibleSus = null;
                await service.UpdatePatient(pessoa);

                if (wasResponsible && oldHouseId > 0 && oldFamilyId > 0)
                {
                    await PromoteNewResponsibleAsync(oldHouseId, oldFamilyId);
                }
            }
        }

        private async Task PromoteNewResponsibleAsync(int houseId, int familyId)
        {
            var remainingPeople = await service.GetPatientsByFamilyAndHouseId(familyId, houseId) ?? [];
            var newResponsible = remainingPeople.FirstOrDefault(patient => !string.IsNullOrWhiteSpace(patient.SusNumber));

            if (newResponsible == null)
            {
                return;
            }

            foreach (var person in remainingPeople)
            {
                person.FamilyResponsibleSus = newResponsible.SusNumber;
                await service.UpdatePatient(person);
            }
        }
    }
}
