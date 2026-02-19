namespace ACS_View.MVVM.Models.Interfaces
{
    public interface IFamilyManager
    {
        Task AddPeopleToFamily(List<int> ids, int houseId, int familyId);
        Task RemovePersonFromFamily(int id);
    }
}
