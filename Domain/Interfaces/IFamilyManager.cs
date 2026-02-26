namespace ACS_View.Domain.Interfaces
{
    public interface IFamilyManager
    {
        Task AddPeopleToFamily(List<int> ids, int houseId, int familyId);
        Task RemovePersonFromFamily(int id);
    }
}
