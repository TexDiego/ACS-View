namespace ACS_View.MVVM.Models.Interfaces
{
    public interface IFamilyManager
    {
        Task AddPeopleToFamily(List<string> susList, int houseId, int familyId);
        Task RemovePersonFromFamily(string sus);
    }
}
