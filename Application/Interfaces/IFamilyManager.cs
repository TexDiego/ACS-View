namespace ACS_View.Application.Interfaces
{
    public interface IFamilyManager
    {
        Task AddPeopleToFamily(List<int> ids, int houseId, int familyId, int responsiblePatientId);
        Task RemovePersonFromFamily(int id);
        Task RemoveFamilyAsync(int houseId, int localNumber);
        Task MoveFamilyAsync(int familyRecordId, int destinationHouseId);
    }
}
