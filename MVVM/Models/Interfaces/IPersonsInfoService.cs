namespace ACS_View.MVVM.Models.Interfaces
{
    public interface IPersonsInfoService
    {
        Task<string> GetEnderecoAsync(int id);
        Task<string?> GetComplementoAsync(int id);
    }
}