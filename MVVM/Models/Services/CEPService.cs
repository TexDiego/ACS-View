using System.Text.Json;

namespace ACS_View.MVVM.Models.Services
{
    public class CEPService
    {
        public static async Task<House> BuscarEnderecoPorCep(string cep)
        {
            using var httpClient = new HttpClient();
            string url = $"https://viacep.com.br/ws/{cep}/json/";

            var response = await httpClient.GetStringAsync(url);
            if (!string.IsNullOrEmpty(response))
            {
                var endereco = JsonSerializer.Deserialize<House>(response);
                return endereco;
            }

            return null;
        }
    }
}
