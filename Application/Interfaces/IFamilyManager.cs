namespace ACS_View.Application.Interfaces
{
    public interface IFamilyManager
    {
        Task AddPeopleToFamily(List<int> ids, int houseId, int familyId);
        Task RemovePersonFromFamily(int id);
    }
}
