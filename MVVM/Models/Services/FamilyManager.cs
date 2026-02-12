using ACS_View.MVVM.Models.Interfaces;

namespace ACS_View.MVVM.Models.Services
{
    public class FamilyManager : IFamilyManager
    {
        private readonly IHealthRecordService _healthRecordService;

        public FamilyManager(IHealthRecordService healthRecordService)
        {
            _healthRecordService = healthRecordService;
        }

        public async Task AddPeopleToFamily(List<string> susList, int houseId, int familyId)
        {
            foreach (var sus in susList)
            {
                var pessoa = await _healthRecordService.GetRecordBySusAsync(sus);
                if (pessoa != null)
                {
                    pessoa.FamilyId = familyId;
                    pessoa.HouseId = houseId;
                    await _healthRecordService.UpdateRecordAsync(pessoa);
                }
            }
        }

        public async Task RemovePersonFromFamily(string sus)
        {
            var pessoa = await _healthRecordService.GetRecordBySusAsync(sus);
            if (pessoa != null)
            {
                pessoa.FamilyId = 0;
                pessoa.HouseId = 0;
                await _healthRecordService.UpdateRecordAsync(pessoa);
            }
        }
    }
}
