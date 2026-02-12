namespace ACS_View.MVVM.Models.Interfaces
{
    public interface IPersonsInfoService
    {
        Task<string> GetEnderecoAsync(string susNumber);
        Task<string> GetComplementoAsync(string susNumber);
        Task<bool> TemComplementoAsync(string susNumber);
    }
}