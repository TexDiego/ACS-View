using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ACS_View.UseCases.Services
{
    internal class HouseService(IHouseRepository repository) : IHouseService
    {
        public Task<List<House>> GetTerritorialHousesAsync(string? search = null) => repository.GetAllAsync(search);

        public async Task<List<House>> SearchTerritorialAsync(string street, int? start, int? end, HouseParity parity)
        {
            if (string.IsNullOrWhiteSpace(street)) throw new ArgumentException("Informe a rua / logradouro.");
            TerritorialRules.ValidateRange(start, end);
            var candidates = await repository.SearchTerritorialAsync(street);
            return await Task.Run(() => candidates.Where(h => TerritorialRules.MatchesNumber(h.NumeroCasa, start, end, parity))
                .OrderBy(TerritorialRules.Street, NaturalCodeComparer.Instance).ThenBy(h => h.NumeroCasa, NaturalCodeComparer.Instance).ThenBy(h => h.CasaId).ToList());
        }

        public Task<List<House>> GetTerritorialSelectionAsync(IReadOnlyCollection<int> ids) => repository.GetTerritorialSelectionAsync(ids);

        public async Task<int> ApplyTerritoryAsync(IReadOnlyCollection<House> reviewed, string sector, string block, bool onlyUnclassified)
        {
            if (reviewed.Count == 0) throw new ArgumentException("Selecione ao menos uma residência.");
            if (string.IsNullOrWhiteSpace(sector) || string.IsNullOrWhiteSpace(block))
                throw new ArgumentException("Informe o setor censitário e a quadra.");
            var count = await repository.ApplyTerritoryAsync(reviewed, sector.Trim(), block.Trim(), onlyUnclassified);
            if (count > 0) DataChangeTracker.MarkHousesChanged();
            return count;
        }

        public async Task DeleteHouseAsync(int id)
        {
            try
            {
                var house = await GetHouseByIdAsync(id);
                if (house != null)
                {
                    await repository.DeleteAsync(house);
                    DataChangeTracker.MarkHousesChanged();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao deletar do banco de dados: {ex.Message}");
                throw;
            }
        }

        public async Task<List<House>> GetAllHousesAsync()
        {
            try
            {
                return await repository.GetAllAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao buscar casas: {ex.Message}");
                return [];
            }
        }

        public Task<PagedResultDto<HouseListItemDto>> GetHouseListAsync(string? search, int skip, int take, string? filterKey = null, HouseNumberFilterDto? numberFilter = null)
        {
            return repository.GetListAsync(search, skip, take, filterKey, numberFilter);
        }

        public Task<House?> GetHouseByIdAsync(int id)
        {
            return repository.GetByIdAsync(id);
        }

        public async Task<House?> GetHouseByPatientIdAsync(int id)
        {
            try
            {
                return await repository.GetByPatientIdAsync(id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao buscar casa pelo Id: {ex.Message}");
                throw new Exception("Erro ao buscar informações da casa.");
            }
        }

        public Task<int> GetMaxIdAsync()
        {
            return repository.GetMaxIdAsync();
        }

        public Task<int> GetTotalCountAsync()
        {
            return repository.GetTotalCountAsync();
        }

        public async Task SaveHouseAsync(House house)
        {
            NormalizeHouseFields(house);
            await repository.InsertAsync(house);
            DataChangeTracker.MarkHousesChanged();
        }

        public async Task UpdateHouseAsync(House house)
        {
            NormalizeHouseFields(house);
            await repository.UpdateAsync(house);
            DataChangeTracker.MarkHousesChanged();
        }

        private static void NormalizeHouseFields(House house)
        {
            house.SetorCensitario = TerritorialRules.OptionalCode(house.SetorCensitario);
            house.NumeroQuadra = TerritorialRules.OptionalCode(house.NumeroQuadra);
            house.TipoLogradouro = NormalizeAddressOptional(house.TipoLogradouro);
            house.Rua = NormalizeAddressRequired(house.Rua, "Logradouro");
            house.Bairro = NormalizeAddressRequired(house.Bairro, "Bairro");
            house.Cidade = NormalizeAddressRequired(house.Cidade, "Cidade");
            house.Estado = NormalizeState(house.Estado);
            house.Pais = NormalizeAddressRequired(house.Pais, "Pais");
            house.SearchRua = SearchTextNormalizer.Normalize(BuildStreetDisplay(house.TipoLogradouro, house.Rua));
            house.SearchComplemento = SearchTextNormalizer.Normalize(house.Complemento);
        }

        private static string BuildStreetDisplay(string? streetType, string? street)
        {
            return string.Join(" ", new[] { streetType, street }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim()));
        }

        private static string NormalizeState(string state)
        {
            if (string.IsNullOrWhiteSpace(state))
            {
                throw new ArgumentException("Estado é obrigatório.");
            }

            var trimmed = state.Trim();
            return trimmed.Length == 2
                ? trimmed.ToUpperInvariant()
                : NormalizeAddressRequired(trimmed, "Estado");
        }

        private static string NormalizeAddressRequired(string? value, string fieldName)
        {
            var normalized = NormalizeAddressOptional(value);
            return string.IsNullOrWhiteSpace(normalized)
                ? throw new ArgumentException($"{fieldName} é obrigatório.")
                : normalized;
        }

        private static string NormalizeAddressOptional(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var compact = Regex.Replace(value.Trim(), @"\s+", " ");
            return CapitalizeAddress(compact);
        }

        private static string CapitalizeAddress(string value)
        {
            var culture = CultureInfo.GetCultureInfo("pt-BR");
            var result = new char[value.Length];
            var capitalizeNext = true;

            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index] == '\u2019' ? '\'' : value[index];
                if (char.IsLetter(character))
                {
                    var lower = char.ToLower(character, culture);
                    result[index] = capitalizeNext ? char.ToUpper(lower, culture) : lower;
                    capitalizeNext = false;
                    continue;
                }

                result[index] = character;
                capitalizeNext = character is ' ' or '-' or '\'' or '/' or '.';
            }

            return new string(result);
        }
    }
}
