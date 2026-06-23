using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using System.Diagnostics;

namespace ACS_View.UseCases.Services
{
    internal class HouseService(IHouseRepository repository) : IHouseService
    {
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

        public Task<PagedResultDto<HouseListItemDto>> GetHouseListAsync(string? search, int skip, int take)
        {
            return repository.GetListAsync(search, skip, take);
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
            house.Rua = PatientNameRules.NormalizeRequired(house.Rua, "Logradouro");
            house.Bairro = PatientNameRules.NormalizeRequired(house.Bairro, "Bairro");
            house.Cidade = PatientNameRules.NormalizeRequired(house.Cidade, "Cidade");
            house.Estado = NormalizeState(house.Estado);
            house.Pais = PatientNameRules.NormalizeRequired(house.Pais, "País");
            house.SearchRua = SearchTextNormalizer.Normalize(house.Rua);
            house.SearchComplemento = SearchTextNormalizer.Normalize(house.Complemento);
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
                : PatientNameRules.NormalizeRequired(trimmed, "Estado");
        }
    }
}
