using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.Interfaces;
using ACS_View.Domain.ValueObjects;
using SQLite;
using System.Diagnostics;

namespace ACS_View.UseCases.Services
{
    internal class DatabaseService(SQLiteAsyncConnection database) : IDatabaseService
    {
        public SQLiteAsyncConnection Connection { get; } = database;

        public async Task InitializeAsync()
        {
            try
            {
                await CreateDomainTablesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao inicializar banco: {ex.Message}");
                throw;
            }
        }

        public Task<User> GetUserByUsernameAsync(string username)
        {
            return Connection.Table<User>().FirstOrDefaultAsync(x => x.Username == username);
        }

        public Task<int> UpdateUserAsync(User user)
        {
            return Connection.UpdateAsync(user);
        }

        private async Task CreateDomainTablesAsync()
        {
            await Connection.CreateTablesAsync<Patient, Vaccines>();
            await Connection.CreateTablesAsync<Note, House>();
            await Connection.CreateTablesAsync<Family, User>();
            await Connection.CreateTablesAsync<CidCategory, CidChapter>();
            await Connection.CreateTablesAsync<CidGroup, CidSubcategory>();
            await Connection.CreateTablesAsync<Visits, PatientCID>();
            await Connection.CreateTablesAsync<CommonConditions, PatientConditions>();

            await CreateIndexesAsync();
        }

        private async Task CreateIndexesAsync()
        {
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_Name ON Patient(Name COLLATE NOCASE)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_SusNumber ON Patient(SusNumber COLLATE NOCASE)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_HouseId ON Patient(HouseId)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_Family_House ON Patient(FamilyId, HouseId)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_House_Rua_Numero ON House(Rua COLLATE NOCASE, NumeroCasa)");
        }
    }
}
