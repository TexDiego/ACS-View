using ACS_View.Domain.Entities;
using ACS_View.Domain.Interfaces;
using ACS_View.Domain.ValueObjects;
using ACS_View.UseCases.DTOs;
using SQLite;
using System.Diagnostics;

namespace ACS_View.UseCases.Services
{
    internal class HouseService(IDatabaseService _databaseService) : IHouseService
    {
        private readonly SQLiteAsyncConnection _connection = _databaseService.Connection;

        public async Task DeleteHouseAsync(int id)
        {
            try
            {
                var house = await GetHouseByIdAsync(id);
                if (house != null)
                {
                    await _connection.DeleteAsync(house);
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
                return await _connection.Table<House>().ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao buscar casas: {ex.Message}");
                return [];
            }
        }

        public async Task<PagedResultDto<HouseListItemDto>> GetHouseListAsync(string? search, int skip, int take)
        {
            try
            {
                skip = Math.Max(skip, 0);
                take = Math.Clamp(take, 1, 100);

                var searchCriteria = BuildSearchCriteria(search);
                var totalCount = await _connection.ExecuteScalarAsync<int>(
                    $"SELECT COUNT(*) FROM House {searchCriteria.WhereClause}",
                    [.. searchCriteria.Parameters]);

                searchCriteria.Parameters.Add(take);
                searchCriteria.Parameters.Add(skip);

                var items = await _connection.QueryAsync<HouseListItemDto>(
                    $"""
                    SELECT CasaId, Rua, NumeroCasa, Complemento
                    FROM House
                    {searchCriteria.WhereClause}
                    ORDER BY Rua COLLATE NOCASE, CAST(NumeroCasa AS INTEGER), NumeroCasa COLLATE NOCASE, Complemento COLLATE NOCASE
                    LIMIT ? OFFSET ?
                    """,
                    [.. searchCriteria.Parameters]);

                return new PagedResultDto<HouseListItemDto>
                {
                    Items = items,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao buscar casas paginadas: {ex.Message}");
                return new PagedResultDto<HouseListItemDto>();
            }
        }

        public async Task<House?> GetHouseByIdAsync(int id)
        {
            try
            {
                return await _connection.FindAsync<House>(id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao buscar casa por ID: {ex.Message}");
                throw;
            }
        }

        public async Task<House?> GetHouseByPatientIdAsync(int id)
        {
            try
            {
                var query = @"
                    SELECT h.*
                    FROM House h
                    JOIN Patient r ON h.CasaId = r.HouseId
                    WHERE r.Id = ?";

                var result = await _connection.FindWithQueryAsync<House>(query, id);
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao buscar casa pelo Id: {ex.Message}");
                throw new Exception("Erro ao buscar informações da casa.");
            }
        }

        public async Task<int> GetMaxIdAsync()
        {
            try
            {
                var maxId = await _connection.ExecuteScalarAsync<int?>("SELECT MAX(CasaId) FROM House");
                return maxId ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter o ID máximo: {ex.Message}");
                throw;
            }
        }

        public async Task<int> GetTotalCountAsync()
        {
            try
            {
                var count = await _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM House");
                return count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao contar casas: {ex.Message}");
                throw;
            }
        }

        public async Task SaveHouseAsync(House house)
        {
            try
            {
                await _connection.InsertAsync(house);
                DataChangeTracker.MarkHousesChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar casa: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateHouseAsync(House house)
        {
            try
            {
                await _connection.UpdateAsync(house);
                DataChangeTracker.MarkHousesChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar casa: {ex.Message}");
                throw;
            }
        }

        private static HouseSearchCriteria BuildSearchCriteria(string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return new HouseSearchCriteria();
            }

            var searchParts = search.Split([' '], StringSplitOptions.RemoveEmptyEntries);
            var possibleNumber = searchParts.LastOrDefault();
            var hasNumber = int.TryParse(possibleNumber, out _);
            var streetSearch = string.Join(" ", searchParts.Take(searchParts.Length - (hasNumber ? 1 : 0))).Trim();
            var parameters = new List<object>();
            var clauses = new List<string>();

            if (!string.IsNullOrWhiteSpace(streetSearch))
            {
                clauses.Add("Rua LIKE ? COLLATE NOCASE");
                parameters.Add($"%{streetSearch}%");
            }

            if (hasNumber && !string.IsNullOrWhiteSpace(possibleNumber))
            {
                clauses.Add("NumeroCasa = ?");
                parameters.Add(possibleNumber);
            }

            if (clauses.Count == 0)
            {
                clauses.Add("(Rua LIKE ? COLLATE NOCASE OR NumeroCasa LIKE ? COLLATE NOCASE OR Complemento LIKE ? COLLATE NOCASE)");
                var likeSearch = $"%{search.Trim()}%";
                parameters.Add(likeSearch);
                parameters.Add(likeSearch);
                parameters.Add(likeSearch);
            }

            return new HouseSearchCriteria
            {
                WhereClause = $"WHERE {string.Join(" AND ", clauses)}",
                Parameters = parameters
            };
        }

        private sealed class HouseSearchCriteria
        {
            public string WhereClause { get; init; } = string.Empty;
            public List<object> Parameters { get; init; } = [];
        }
    }
}
