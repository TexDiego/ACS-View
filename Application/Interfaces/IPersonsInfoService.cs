namespace ACS_View.Application.Interfaces
{
    public interface IPersonsInfoService
    {
        Task<string> GetEnderecoAsync(int id);
        Task<string?> GetComplementoAsync(int id);
        Task<(string Endereco, string Complemento)> GetAddressInfoAsync(int id);
    }
}
