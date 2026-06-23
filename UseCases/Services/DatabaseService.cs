using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
using ACS_View.Application.Interfaces;
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

        public Task<User?> GetUserByUsernameAsync(string username)
        {
            return Connection.Table<User>().FirstOrDefaultAsync(x => x.Username == username);
        }

        public Task<int> InsertUserAsync(User user)
        {
            return Connection.InsertAsync(user);
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

            await MigrateUserTableAsync();
            await BackfillSearchColumnsAsync();
            await CreateIndexesAsync();
        }

        private async Task MigrateUserTableAsync()
        {
            await EnsureColumnAsync("User", nameof(User.PasswordHash), "TEXT NOT NULL DEFAULT ''");
            await EnsureColumnAsync("User", nameof(User.PasswordSalt), "TEXT NOT NULL DEFAULT ''");
            await EnsureColumnAsync("User", nameof(User.PasswordHashVersion), "INTEGER NOT NULL DEFAULT 1");
            await EnsureColumnAsync("User", nameof(User.SecurityAnswerHash), "TEXT NOT NULL DEFAULT ''");
            await EnsureColumnAsync("User", nameof(User.SecurityAnswerSalt), "TEXT NOT NULL DEFAULT ''");
        }

        private async Task EnsureColumnAsync(string tableName, string columnName, string definition)
        {
            var columns = await Connection.QueryAsync<TableColumnInfo>($"PRAGMA table_info([{tableName}])");
            if (columns.Any(column => string.Equals(column.Name, columnName, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            await Connection.ExecuteAsync($"ALTER TABLE [{tableName}] ADD COLUMN {columnName} {definition}");
        }

        private async Task CreateIndexesAsync()
        {
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_Name ON Patient(Name COLLATE NOCASE)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_SearchName ON Patient(SearchName)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_SearchMotherName ON Patient(SearchMotherName)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_SearchFatherName ON Patient(SearchFatherName)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_SusNumber ON Patient(SusNumber COLLATE NOCASE)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_HouseId ON Patient(HouseId)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_Family_House ON Patient(FamilyId, HouseId)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_House_Rua_Numero ON House(Rua COLLATE NOCASE, NumeroCasa)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_House_SearchRua_Numero ON House(SearchRua, NumeroCasa)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_House_SearchComplemento ON House(SearchComplemento)");
        }

        private async Task BackfillSearchColumnsAsync()
        {
            var patients = await Connection.Table<Patient>().ToListAsync();
            foreach (var patient in patients)
            {
                var searchName = SearchTextNormalizer.Normalize(patient.Name);
                var searchMotherName = SearchTextNormalizer.Normalize(patient.MotherName);
                var searchFatherName = SearchTextNormalizer.Normalize(patient.FatherName);

                if (patient.SearchName == searchName &&
                    patient.SearchMotherName == searchMotherName &&
                    patient.SearchFatherName == searchFatherName)
                {
                    continue;
                }

                patient.SearchName = searchName;
                patient.SearchMotherName = searchMotherName;
                patient.SearchFatherName = searchFatherName;
                await Connection.UpdateAsync(patient);
            }

            var houses = await Connection.Table<House>().ToListAsync();
            foreach (var house in houses)
            {
                var searchRua = SearchTextNormalizer.Normalize(house.Rua);
                var searchComplemento = SearchTextNormalizer.Normalize(house.Complemento);

                if (house.SearchRua == searchRua &&
                    house.SearchComplemento == searchComplemento)
                {
                    continue;
                }

                house.SearchRua = searchRua;
                house.SearchComplemento = searchComplemento;
                await Connection.UpdateAsync(house);
            }
        }

        private sealed class TableColumnInfo
        {
            [Column("name")]
            public string Name { get; set; } = string.Empty;
        }
    }
}
