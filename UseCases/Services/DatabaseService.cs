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
        private readonly SemaphoreSlim _initializeLock = new(1, 1);
        private bool _initialized;

        public SQLiteAsyncConnection Connection { get; } = database;

        public async Task InitializeAsync()
        {
            if (_initialized)
            {
                return;
            }

            await _initializeLock.WaitAsync();
            try
            {
                if (_initialized)
                {
                    return;
                }

                await CreateDomainTablesAsync();
                _initialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao inicializar banco: {ex.Message}");
                throw;
            }
            finally
            {
                _initializeLock.Release();
            }
        }

        public Task<User?> GetUserByUsernameAsync(string username)
        {
            return Connection.Table<User>().FirstOrDefaultAsync(x => x.Username == username);
        }

        public Task<User?> GetUserByIdAsync(int id)
        {
            return Connection.Table<User>().FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task<int> InsertUserAsync(User user)
        {
            return Connection.InsertAsync(user);
        }

        public Task<int> UpdateUserAsync(User user)
        {
            return Connection.UpdateAsync(user);
        }

        public async Task ClaimLegacyDataForUserAsync(int userId)
        {
            if (userId <= 0)
            {
                return;
            }

            foreach (var tableName in UserScopedTables)
            {
                await Connection.ExecuteAsync($"UPDATE [{tableName}] SET UserId = ? WHERE UserId = 0", userId);
            }
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

            await MigratePatientTableAsync();
            await MigrateHouseTableAsync();
            await MigrateUserScopedTablesAsync();
            await MigrateUserTableAsync();
            await BackfillSearchColumnsAsync();
            await BackfillFamilyResponsibleAsync();
            await CreateIndexesAsync();
        }

        private async Task MigratePatientTableAsync()
        {
            await EnsureColumnAsync("Patient", nameof(Patient.FamilyResponsibleSus), "TEXT NULL");
            await EnsureColumnAsync("Patient", nameof(Patient.MotherPatientId), "INTEGER NULL");
            await EnsureColumnAsync("Patient", nameof(Patient.FatherPatientId), "INTEGER NULL");
            await EnsureColumnAsync("Patient", nameof(Patient.FamilyResponsiblePatientId), "INTEGER NULL");
            await EnsureColumnAsync("Patient", nameof(Patient.IsActive), "INTEGER NOT NULL DEFAULT 1");
            await EnsureColumnAsync("Patient", nameof(Patient.StatusReason), "TEXT NOT NULL DEFAULT ''");
            await EnsureColumnAsync("Patient", nameof(Patient.StatusChangedAt), "TEXT NULL");
            await BackfillPatientStatusAsync();
        }

        private async Task BackfillPatientStatusAsync()
        {
            await Connection.ExecuteAsync("UPDATE Patient SET IsActive = 1 WHERE IsActive IS NULL");
            await Connection.ExecuteAsync("UPDATE Patient SET StatusReason = '' WHERE StatusReason IS NULL");
        }

        private async Task MigrateHouseTableAsync()
        {
            await EnsureColumnAsync("House", nameof(House.TipoLogradouro), "TEXT NOT NULL DEFAULT ''");
        }

        private async Task MigrateUserScopedTablesAsync()
        {
            foreach (var tableName in UserScopedTables)
            {
                await EnsureColumnAsync(tableName, "UserId", "INTEGER NOT NULL DEFAULT 0");
            }
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
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_UserId_Name ON Patient(UserId, Name COLLATE NOCASE)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_UserId_IsActive ON Patient(UserId, IsActive)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_UserId_IsActive_Name ON Patient(UserId, IsActive, Name COLLATE NOCASE)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_SearchName ON Patient(SearchName)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_SearchMotherName ON Patient(SearchMotherName)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_SearchFatherName ON Patient(SearchFatherName)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_SusNumber ON Patient(SusNumber COLLATE NOCASE)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_FamilyResponsibleSus ON Patient(FamilyResponsibleSus COLLATE NOCASE)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_MotherPatientId ON Patient(MotherPatientId)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_FatherPatientId ON Patient(FatherPatientId)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_FamilyResponsiblePatientId ON Patient(FamilyResponsiblePatientId)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_HouseId ON Patient(HouseId)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_UserId_HouseId ON Patient(UserId, HouseId)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_Family_House ON Patient(FamilyId, HouseId)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_UserId_Family_House ON Patient(UserId, FamilyId, HouseId)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_PatientCID_UserId_PatientId ON PatientCID(UserId, PatientId)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_PatientCID_UserId_CidId ON PatientCID(UserId, CidId)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_PatientConditions_UserId_PatientId ON PatientConditions(UserId, PatientId)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_House_Tipo_Rua_Numero ON House(TipoLogradouro COLLATE NOCASE, Rua COLLATE NOCASE, NumeroCasa)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_House_UserId_Tipo_Rua_Numero ON House(UserId, TipoLogradouro COLLATE NOCASE, Rua COLLATE NOCASE, NumeroCasa)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_House_Rua_Numero ON House(Rua COLLATE NOCASE, NumeroCasa)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_House_SearchRua_Numero ON House(SearchRua, NumeroCasa)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_House_UserId_SearchRua_Numero ON House(UserId, SearchRua, NumeroCasa)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_House_SearchComplemento ON House(SearchComplemento)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Note_UserId_CreationDate ON Note(UserId, CreationDate)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Visits_UserId_Date ON Visits(UserId, Date)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Family_UserId_IdFamilia ON Family(UserId, IdFamilia)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_CidSubcategory_Code ON CidSubcategory(Code COLLATE NOCASE)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_CidSubcategory_Description ON CidSubcategory(Description COLLATE NOCASE)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_CidCategory_Code ON CidCategory(Code COLLATE NOCASE)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_CidGroup_Code ON CidGroup(Code COLLATE NOCASE)");
            await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_CidChapter_Code ON CidChapter(Code COLLATE NOCASE)");
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
                var searchRua = SearchTextNormalizer.Normalize(BuildStreetDisplay(house.TipoLogradouro, house.Rua));
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

        private async Task BackfillFamilyResponsibleAsync()
        {
            var patients = await Connection.Table<Patient>().ToListAsync();
            var familyGroups = patients
                .Where(patient => patient.HouseId > 0 && patient.FamilyId > 0)
                .GroupBy(patient => new { patient.HouseId, patient.FamilyId });

            foreach (var familyGroup in familyGroups)
            {
                var groupPatients = familyGroup.ToList();
                var memberSusNumbers = groupPatients
                    .Select(patient => patient.SusNumber)
                    .Where(sus => !string.IsNullOrWhiteSpace(sus))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var validResponsibleSus = groupPatients
                    .Select(patient => patient.FamilyResponsibleSus)
                    .Where(sus => !string.IsNullOrWhiteSpace(sus) && memberSusNumbers.Contains(sus))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var responsibleSus = validResponsibleSus.Count == 1
                    ? validResponsibleSus[0]
                    : groupPatients.FirstOrDefault(patient => !string.IsNullOrWhiteSpace(patient.SusNumber))?.SusNumber;

                if (string.IsNullOrWhiteSpace(responsibleSus))
                {
                    continue;
                }

                foreach (var patient in groupPatients)
                {
                    if (string.Equals(patient.FamilyResponsibleSus, responsibleSus, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    patient.FamilyResponsibleSus = responsibleSus;
                    await Connection.UpdateAsync(patient);
                }
            }
        }

        private sealed class TableColumnInfo
        {
            [Column("name")]
            public string Name { get; set; } = string.Empty;
        }

        private static readonly string[] UserScopedTables =
        [
            "Patient",
            "House",
            "Family",
            "Note",
            "Visits",
            "Vaccines",
            "PatientCID",
            "PatientConditions"
        ];

        private static string BuildStreetDisplay(string? streetType, string? street)
        {
            return string.Join(" ", new[] { streetType, street }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim()));
        }
    }
}
