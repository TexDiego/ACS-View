# Auditoria estática anterior à remodelação - 2026-10-08
Checkout inicialmente limpo. Inventário derivado do código, não de um banco pessoal. Atributos C# não demonstram FKs reais.
## Entidades, colunas, chaves e índices declarados
### CareNotification.cs
```csharp
public class CareNotification
[PrimaryKey, AutoIncrement]
public int Id { get; set; }
[Indexed]
public int UserId { get; set; }
[Indexed]
public int? PatientId { get; set; }
public CareNotificationCategory Category { get; set; }
public CareNotificationType Type { get; set; }
public CareNotificationPriority Priority { get; set; }
public CareNotificationStatus Status { get; set; } = CareNotificationStatus.Active;
public string Title { get; set; } = string.Empty;
public string Message { get; set; } = string.Empty;
public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
public DateTime? DueDate { get; set; }
public DateTime? ResolvedAt { get; set; }
public DateTime? DismissedAt { get; set; }
public DateTime? SnoozedUntil { get; set; }
[Indexed]
public string UniqueKey { get; set; } = string.Empty;
public string SourceRule { get; set; } = string.Empty;
public string MetadataJson { get; set; } = string.Empty;
```
### CepAddressCache.cs
```csharp
public class CepAddressCache
[PrimaryKey]
public string Cep { get; set; } = string.Empty;
public string Rua { get; set; } = string.Empty;
public string TipoLogradouro { get; set; } = string.Empty;
public string Bairro { get; set; } = string.Empty;
public string Cidade { get; set; } = string.Empty;
public string Estado { get; set; } = string.Empty;
public string Pais { get; set; } = "Brasil";
public DateTime UpdatedAt { get; set; }
```
### Family.cs
```csharp
public class Family
[PrimaryKey]
public int IdFamilia { get; set; }
public int UserId { get; set; }
public int IdPessoa { get; set; }
```
### House.cs
```csharp
public class House
[PrimaryKey, AutoIncrement]
public int CasaId { get; set; }
public int UserId { get; set; }
[JsonPropertyName("cep")]
public string CEP { get; set; } = string.Empty;
[JsonPropertyName("logradouro")]
public string Rua { get; set; } = string.Empty;
public string TipoLogradouro { get; set; } = string.Empty;
public string NumeroCasa { get; set; } = string.Empty;
public string SearchRua { get; set; } = string.Empty;
public string SearchComplemento { get; set; } = string.Empty;
[JsonPropertyName("bairro")]
public string Bairro { get; set; } = string.Empty;
[JsonPropertyName("localidade")]
public string Cidade { get; set; } = string.Empty;
[JsonPropertyName("uf")]
public string Estado { get; set; } = string.Empty;
public string Pais { get; set; } = "Brasil";
public string Complemento { get; set; } = string.Empty;
public bool PossuiComplemento { get; set; } = false;
```
### ImportHistory.cs
```csharp
public class ImportHistory
[PrimaryKey, AutoIncrement] public int Id { get; set; }
[Indexed] public int UserId { get; set; }
public string FileName { get; set; } = string.Empty;
public string StartedAt { get; set; } = string.Empty;
public string FinishedAt { get; set; } = string.Empty;
public string Status { get; set; } = string.Empty;
public string ReportJson { get; set; } = string.Empty;
```
### Note.cs
```csharp
public class Note
[PrimaryKey, AutoIncrement]
public int Id { get; set; }
public int UserId { get; set; }
public string Content { get; set; }
public DateTime CreationDate { get; set; } = DateTime.Now;
public DateTime? NotifyOn { get; set; } = null;
public string? ReminderMessage { get; set; }
```
### Patient.cs
```csharp
public class Patient
[PrimaryKey, AutoIncrement]
public int Id { get; set; }
public int UserId { get; set; }
public int FamilyId { get; set; } = -1;
public int HouseId { get; set; } = -1;
[Ignore]
[System.Text.Json.Serialization.JsonIgnore]
[Ignore]
[System.Text.Json.Serialization.JsonIgnore]
public string? FamilyResponsibleSus { get; set; }
public int? MotherPatientId { get; set; }
public int? FatherPatientId { get; set; }
public int? FamilyResponsiblePatientId { get; set; }
public string Sexo { get; set; } = "Indeterminado";
public string Name { get; set; } = string.Empty;
public string MotherName { get; set; } = string.Empty;
public string FatherName { get; set; } = string.Empty;
public string SearchName { get; set; } = string.Empty;
public string SearchMotherName { get; set; } = string.Empty;
public string SearchFatherName { get; set; } = string.Empty;
public DateTime BirthDate { get; set; } = DateTime.Today;
public string Observacao { get; set; } = string.Empty;
public bool IsActive { get; set; } = true;
public string StatusReason { get; set; } = string.Empty;
public DateTime? StatusChangedAt { get; set; }
```
### PatientBolsaFamilia.cs
```csharp
public class PatientBolsaFamilia
[PrimaryKey, AutoIncrement]
public int Id { get; set; }
[Indexed("UX_PatientBolsaFamilia_User_Patient", 1, Unique = true)]
public int UserId { get; set; }
[Indexed("UX_PatientBolsaFamilia_User_Patient", 2, Unique = true)]
public int PatientId { get; set; }
[Indexed]
public int ResponsiblePatientId { get; set; }
public string NisNumber { get; set; } = string.Empty;
```
### PatientInsulinDependency.cs
```csharp
public class PatientInsulinDependency
[PrimaryKey, AutoIncrement]
public int Id { get; set; }
[Indexed("UX_PatientInsulinDependency_User_Patient", 1, Unique = true)]
public int UserId { get; set; }
[Indexed("UX_PatientInsulinDependency_User_Patient", 2, Unique = true)]
public int PatientId { get; set; }
```
### PatientPregnancy.cs
```csharp
public class PatientPregnancy
[PrimaryKey, AutoIncrement]
public int Id { get; set; }
[Indexed]
public int UserId { get; set; }
[Indexed]
public int PatientId { get; set; }
public DateTime? LastMenstrualPeriod { get; set; }
public DateTime? ExpectedBirthDate { get; set; }
public PregnancyStatus Status { get; set; } = PregnancyStatus.Active;
public PregnancyRisk ManualRisk { get; set; } = PregnancyRisk.NotInformed;
public int? InformedChildrenCount { get; set; }
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
public DateTime? EndedAt { get; set; }
public PregnancyEndType? EndType { get; set; }
public string Notes { get; set; } = string.Empty;
```
### PatientVaccineDose.cs
```csharp
public class PatientVaccineDose
[PrimaryKey, AutoIncrement]
public int Id { get; set; }
[Indexed]
public int UserId { get; set; }
[Indexed]
public int PatientId { get; set; }
[Indexed]
public string DoseKey { get; set; } = string.Empty;
public bool IsApplied { get; set; }
public DateTime? AppliedAt { get; set; }
public string LotNumber { get; set; } = string.Empty;
public string Notes { get; set; } = string.Empty;
public DateTime CreatedAt { get; set; } = DateTime.Now;
```
### User.cs
```csharp
public class User
[PrimaryKey, AutoIncrement]
public int Id { get; set; }
public string Username { get; set; } = string.Empty;
public string Password { get; set; } = string.Empty;
public string PasswordHash { get; set; } = string.Empty;
public string PasswordSalt { get; set; } = string.Empty;
public int PasswordHashVersion { get; set; } = 1;
public string SecurityQuestion { get; set; } = string.Empty;
public string SecurityAnswer { get; set; } = string.Empty;
public string SecurityAnswerHash { get; set; } = string.Empty;
public string SecurityAnswerSalt { get; set; } = string.Empty;
public string RecoveryCodeHash { get; set; } = string.Empty;
public string RecoveryCodeSalt { get; set; } = string.Empty;
public int FailedLoginAttempts { get; set; }
public long LoginBlockedUntilUtc { get; set; }
public int FailedRecoveryAttempts { get; set; }
public long RecoveryBlockedUntilUtc { get; set; }
public int CredentialRevision { get; set; }
public string BiometricTokenHash { get; set; } = string.Empty;
```
### Vaccines.cs
```csharp
public class Vaccines
[PrimaryKey]
public int Id { get; set; }
public int UserId { get; set; }
public DateTime BirthDate { get; set; }
public bool IsPregnant { get; set; } = false;
public bool BCG_Infantil { get; set; } = false;
public bool HepatitisBAoNascer_Infantil { get; set; } = false;
public bool Penta1_Infantil { get; set; } = false;
public bool VIP1_Infantil { get; set; } = false;
public bool Pneumo10_1_Infantil { get; set; } = false;
public bool VRH1_Infantil { get; set; } = false;
public bool MeningoC1_Infantil { get; set; } = false;
public bool Penta2_Infantil { get; set; } = false;
public bool VIP2_Infantil { get; set; } = false;
public bool Pneumo10_2_Infantil { get; set; } = false;
public bool VRH2_Infantil { get; set; } = false;
public bool MeningoC2_Infantil { get; set; } = false;
public bool Penta3_Infantil { get; set; } = false;
public bool VIP3_Infantil { get; set; } = false;
public bool Covid1_Infantil { get; set; } = false;
public bool Covid2_Infantil { get; set; } = false;
public bool FebreAmarela1_Infantil { get; set; } = false;
public bool Pneumo10_3_Infantil { get; set; } = false;
public bool MeningoC3_Infantil { get; set; } = false;
public bool TripliceViral_Infantil { get; set; } = false;
public bool DTP1_Infantil { get; set; } = false;
public bool VIP4_Infantil { get; set; } = false;
public bool HepatiteA_Infantil { get; set; } = false;
public bool TetraViral_Infantil { get; set; } = false;
public bool DTP2_Infantil { get; set; } = false;
public bool FebreAmarela2_Infantil { get; set; } = false;
public bool Varicela_Infantil { get; set; } = false;
public bool FebreAmarela3_Infantil { get; set; } = false;
public bool Pneumo23_Infantil { get; set; } = false;
public bool DT_Infantil { get; set; } = false;
public bool HPV_Infantil { get; set; } = false;
public bool HepatiteB_Adolescente { get; set; } = false;
public bool DT_Adolescente { get; set; } = false;
public bool FebreAmarela_Adolescente { get; set; } = false;
public bool TripliceViral_Adolescente { get; set; } = false;
public bool HPV_Adolescente { get; set; } = false;
public bool ACWY_Adolescente { get; set; } = false;
public bool HepatiteB_Adulto { get; set; } = false;
public bool dT_Adulto { get; set; } = false;
public bool FebreAmarela_Adulto { get; set; } = false;
public bool HPV_Adulto { get; set; } = false;
public bool TripliceViral1_Adulto { get; set; } = false;
public bool TripliceViral2_Adulto { get; set; } = false;
public bool dTpa_Adulto { get; set; } = false;
public bool HepatiteB_Idoso { get; set; } = false;
public bool dT_Idoso { get; set; } = false;
public bool FebreAmarela_Idoso { get; set; } = false;
public bool dTpa_Idoso { get; set; } = false;
public bool HepatiteB_Gestante { get; set; } = false;
public bool dT_Gestante { get; set; } = false;
public bool dTpa_Gestante { get; set; } = false;
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
[Ignore]
```
### VisitBatch.cs
```csharp
public class VisitBatch
[PrimaryKey, AutoIncrement]
public int Id { get; set; }
public int UserId { get; set; }
public int HouseId { get; set; }
public int FamilyId { get; set; }
public DateTime VisitDate { get; set; }
public DateTime CreatedAt { get; set; }
public string Notes { get; set; } = string.Empty;
```
### VisitCareLine.cs
```csharp
public class VisitCareLine
[PrimaryKey, AutoIncrement]
public int Id { get; set; }
public int UserId { get; set; }
public int VisitId { get; set; }
public string CareLineType { get; set; } = string.Empty;
```
### Visits.cs
```csharp
public class Visits
[PrimaryKey, AutoIncrement]
public int Id { get; set; }
public int UserId { get; set; }
public int PatientId { get; set; }
public int HouseId { get; set; }
public int FamilyId { get; set; }
public int? BatchId { get; set; }
public DateTime Date { get; set; }
public DateTime VisitDate { get; set; }
public string Status { get; set; } = string.Empty;
public string? Description { get; set; }
public string? Address { get; set; }
public string Notes { get; set; } = string.Empty;
public DateTime CreatedAt { get; set; }
public DateTime? UpdatedAt { get; set; }
```
### CidCategory.cs
```csharp
public class CidCategory
[PrimaryKey, AutoIncrement]
public int Id { get; set; }
public string Code { get; set; } // Ex: "A00"
public string Description { get; set; }
public string GroupCode { get; set; }
```
### CidChapter.cs
```csharp
public class CidChapter
[PrimaryKey, AutoIncrement]
public int Id { get; set; }
public string Code { get; set; } // Ex: "I"
public string InitialCode { get; set; }
public string FinalCode { get; set; }
public string Description { get; set; }
```
### CidGroup.cs
```csharp
public class CidGroup
[PrimaryKey, AutoIncrement]
public int Id { get; set; }
public string Code { get; set; } // Ex: "A00-A09"
public string InitialCode { get; set; }
public string FinalCode { get; set; }
public string Description { get; set; }
public string ChapterCode { get; set; }
```
### CidSubcategory.cs
```csharp
public class CidSubcategory
[PrimaryKey, AutoIncrement]
public int Id { get; set; }
public string Code { get; set; } // Ex: "A00.0"
public string Description { get; set; }
public string CategoryCode { get; set; }
```
### PatientCID.cs
```csharp
public class PatientCID
[PrimaryKey, AutoIncrement]
public int Id { get; set; }
public int UserId { get; set; }
public int PatientId { get; set; }
public int CidId { get; set; }
```
### PatientConditions.cs
```csharp
public class PatientConditions
[PrimaryKey, AutoIncrement]
public int Id { get; set; }
public int UserId { get; set; }
[ForeignKey(nameof(Patient))]
public int? PatientId { get; set; }
public string Description { get; set; }
```
## Consumidores e SQL direto
### Domain\Entities\Patient.cs
12: public int FamilyId { get; set; } = -1;
13: public int HouseId { get; set; } = -1;
15: private string susNumber = string.Empty;
18: public string SusNumber
20: get => susNumber;
21: set => susNumber = SusNumberSet.Join(SusNumberSet.Parse(value));
26: public IReadOnlyList<string> SusNumbers
28: get => SusNumberSet.Parse(susNumber);
29: set => susNumber = SusNumberSet.Join(value);
34: public string PrimarySusNumber => SusNumbers.FirstOrDefault() ?? string.Empty;
36: public void AddSusNumbers(string? numbers) =>
37: SusNumbers = SusNumbers.Concat(SusNumberSet.Parse(numbers)).ToArray();
39: public bool HasSusNumber(string? number) => SusNumberSet.Overlaps(SusNumber, number);
### Domain\Entities\VisitBatch.cs
10: public int HouseId { get; set; }
11: public int FamilyId { get; set; }
### Domain\Entities\Visits.cs
11: public int HouseId { get; set; }
12: public int FamilyId { get; set; }
26: return $"Visita: {Id}\nID Pessoa: {PatientId}\nID Casa: {HouseId}\nID Família: {FamilyId}\nData: {Date.ToShortDateString()}\nDescrição: {Description}";
### Domain\ValueObjects\BolsaFamiliaBeneficiary.cs
7: public string SusNumber { get; set; } = string.Empty;
### Domain\ValueObjects\Pessoa.cs
7: public string SusNumber { get; set; } = string.Empty;
### Domain\ValueObjects\SusNumberSet.cs
5: public static class SusNumberSet
### Infrastructure\Data\SQLite\SQLiteCidRepository.cs
55: var chapters = await _database.QueryAsync<CidChapter>(sql, subcategoryId);
116: return await _database.QueryAsync<CidSearchResultDto>(
144: tran.Insert(item);
153: tran.Insert(item);
162: tran.Insert(item);
171: tran.Insert(item);
177: // Insert list of common conditions into the database in a transaction
182: tran.Insert(item);
### Infrastructure\Data\SQLite\SQLiteConditionsRepository.cs
14: await _connection.ExecuteAsync("DELETE FROM PatientConditions WHERE Id = ? AND UserId = ?", Id, userId);
20: await _connection.Table<PatientConditions>().Where(c => c.PatientId == patientId && c.UserId == userId).DeleteAsync();
38: await _connection.InsertAsync(condition);
49: await _connection.InsertAllAsync(conditions);
### Infrastructure\Data\SQLite\SQLiteHouseRepository.cs
17: return _connection.ExecuteAsync("DELETE FROM House WHERE CasaId = ? AND UserId = ?", house.CasaId, userId);
34: var totalCount = await _connection.ExecuteScalarAsync<int>(
41: var items = await _connection.QueryAsync<HouseListItemDto>(
80: JOIN Patient r ON h.CasaId = r.HouseId
87: return await _connection.FindWithQueryAsync<House>(query, id, userId, userId);
93: var maxId = await _connection.ExecuteScalarAsync<int?>("SELECT MAX(CasaId) FROM House WHERE UserId = ?", userId);
100: return _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM House WHERE UserId = ?", userId);
106: return _connection.InsertAsync(house);
112: return _connection.UpdateAsync(house);
128: AND p.HouseId = House.CasaId
153: AND p.HouseId = House.CasaId
### Infrastructure\Data\SQLite\SQLitePatientBolsaFamiliaRepository.cs
25: var rows = await _connection.QueryAsync<BolsaFamiliaGroupRow>(
32: patient.SusNumber,
58: SusNumber = row.SusNumber ?? string.Empty,
83: await _connection.InsertAsync(benefit);
90: await _connection.UpdateAsync(existing);
99: .DeleteAsync();
109: public string SusNumber { get; set; } = string.Empty;
### Infrastructure\Data\SQLite\SQLitePatientInsulinDependencyRepository.cs
29: await _connection.InsertAsync(new PatientInsulinDependency
42: .DeleteAsync();
### Infrastructure\Data\SQLite\SQLitePatientRepository.cs
20: connection.Execute("DELETE FROM PatientCID WHERE PatientId = ? AND UserId = ?", id, userId);
21: connection.Execute("DELETE FROM PatientConditions WHERE PatientId = ? AND UserId = ?", id, userId);
22: connection.Execute("DELETE FROM PatientBolsaFamilia WHERE PatientId = ? AND UserId = ?", id, userId);
23: connection.Execute("DELETE FROM PatientInsulinDependency WHERE PatientId = ? AND UserId = ?", id, userId);
24: connection.Execute("DELETE FROM PatientPregnancy WHERE PatientId = ? AND UserId = ?", id, userId);
25: connection.Execute("DELETE FROM CareNotification WHERE PatientId = ? AND UserId = ?", id, userId);
26: connection.Execute("DELETE FROM PatientVaccineDose WHERE PatientId = ? AND UserId = ?", id, userId);
27: connection.Execute("DELETE FROM Vaccines WHERE Id = ? AND UserId = ?", id, userId);
28: connection.Execute("DELETE FROM Patient WHERE Id = ? AND UserId = ?", id, userId);
63: OR p.SusNumber LIKE ? COLLATE NOCASE
68: AND h.CasaId = p.HouseId
88: var totalCount = await _connection.ExecuteScalarAsync<int>(
95: var items = await _connection.QueryAsync<PatientListItemDto>(
97: SELECT p.Id, p.SusNumber, p.Name
121: return _connection.QueryAsync<Patient>(
146: return _connection.InsertAsync(patient);
152: return _connection.UpdateAsync(patient);
155: public Task<List<Patient>?> GetByHouseIdAsync(int houseId)
158: return _connection.Table<Patient>().Where(p => p.HouseId == houseId && p.UserId == userId).ToListAsync();
161: public Task<List<Patient>?> GetByFamilyAndHouseIdAsync(int familyId, int houseId)
165: .Where(p => p.FamilyId == familyId && p.HouseId == houseId && p.UserId == userId)
### Infrastructure\Services\CachedCepService.cs
55: return connection.InsertOrReplaceAsync(cache);
### Infrastructure\Services\ShellNavigationService.cs
28: int.TryParse(id?.ToString(), out var houseId) &&
29: houseId > 0 &&
30: await TryShowExistingFamiliesAsync(shell, houseId))
38: private static async Task<bool> TryShowExistingFamiliesAsync(Shell shell, int houseId)
50: .LastOrDefault(candidate => candidate.HouseId == houseId);
56: // Selecting the owning tab restores its stack without pushing another page.
### Infrastructure\Services\SQLiteNoteReminderStore.cs
16: var changed = await database.Connection.ExecuteAsync(
17: "UPDATE Note SET NotifyOn = ?, ReminderMessage = ? WHERE Id = ? AND UserId = ?",
### Application\DTOs\OverallMetricMenuAction.cs
7: RestoreHiddenMetrics,
### Application\DTOs\PatientImportColumnMapDto.cs
7: public string SusNumberColumn { get; set; } = "SUS";
### Application\DTOs\PatientListItemDto.cs
6: public string SusNumber { get; set; } = string.Empty;
### Application\DTOs\VisitBatchRequestDto.cs
7: public int HouseId { get; init; }
8: public int FamilyId { get; init; }
### Application\DTOs\VisitRecordDayGroupDto.cs
19: .GroupBy(item => new { item.Family.HouseId, item.Family.FamilyId })
22: HouseId = family.Key.HouseId,
23: FamilyId = family.Key.FamilyId,
32: .ThenBy(family => family.HouseId)
33: .ThenBy(family => family.FamilyId)
### Application\DTOs\VisitRecordDto.cs
5: public int HouseId { get; init; }
6: public int FamilyId { get; init; }
### Application\DTOs\VisitSuggestionDto.cs
6: public int HouseId { get; init; }
7: public int FamilyId { get; init; }
### Application\Interfaces\IFamilyManager.cs
5: Task AddPeopleToFamily(List<int> ids, int houseId, int familyId, int responsiblePatientId);
### Application\Interfaces\IFamilyService.cs
9: Task<List<Patient>> GetPersonOfFamilyById(int houseId, int familyId);
13: Task<int> GetMaxIdAsync(int houseId);
### Application\Interfaces\IPatientRepository.cs
15: Task<List<Patient>?> GetByHouseIdAsync(int houseId);
16: Task<List<Patient>?> GetByFamilyAndHouseIdAsync(int familyId, int houseId);
### Application\Interfaces\IPatientService.cs
14: Task<List<Patient>?> GetPatientsByHouseId(int houseId);
15: Task<List<Patient>?> GetPatientsByFamilyAndHouseId(int familyId, int houseId);
### Application\Interfaces\IVisitsService.cs
11: Task<List<VisitFamilyMemberOptionDto>> GetFamilyVisitOptionsAsync(int houseId, int familyId, DateTime visitDate);
### Application\Querying\PatientFilterSqlBuilder.cs
65: whereParts.Add("p.HouseId = -1");
68: whereParts.Add("p.FamilyId = -1");
### UseCases\CidSeeder.cs
53: await repository.InsertChaptersAsync(chapters);
54: await repository.InsertGroupsAsync(groups);
55: await repository.InsertCategoriesAsync(categories);
56: await repository.InsertSubcategoriesAsync(subcategories);
98: await repository.InsertCommonConditionsAsync(commonConditions);
### UseCases\Services\AuthService.cs
70: await databaseService.UpdateUserAsync(user);
89: await databaseService.UpdateUserAsync(user);
118: await databaseService.InsertUserAsync(user);
143: await databaseService.UpdateUserAsync(user);
155: await databaseService.UpdateUserAsync(user);
185: await databaseService.UpdateUserAsync(user);
228: await databaseService.UpdateUserAsync(user);
242: await databaseService.UpdateUserAsync(user);
311: await databaseService.UpdateUserAsync(user);
322: await databaseService.UpdateUserAsync(user);
### UseCases\Services\CareNotificationService.cs
192: await _connection.UpdateAsync(notification);
203: await _connection.InsertAsync(notification);
228: await _connection.UpdateAsync(existing);
248: await _connection.UpdateAsync(notification);
271: await _connection.UpdateAsync(notification);
### UseCases\Services\DashboardMetricsService.cs
26: TotalPacientes = await _connection.ExecuteScalarAsync<int>(
30: TotalFamilias = await _connection.ExecuteScalarAsync<int>(
34: SELECT p.HouseId, p.FamilyId
38: AND p.HouseId > 0
39: AND p.FamilyId > 0
40: GROUP BY p.HouseId, p.FamilyId
44: TotalResidenciasVazias = await _connection.ExecuteScalarAsync<int>(
53: AND p.HouseId = h.CasaId
58: TotalSemResidencia = await _connection.ExecuteScalarAsync<int>(
59: $"SELECT COUNT(*) FROM Patient WHERE UserId = ? AND {ActivePatientClause} AND HouseId = -1",
61: TotalBolsaFamilia = await _connection.ExecuteScalarAsync<int>(
71: TotalIdosos = await _connection.ExecuteScalarAsync<int>(
75: TotalCriancasMenoresDe6 = await _connection.ExecuteScalarAsync<int>(
80: TotalMulheres25a64 = await _connection.ExecuteScalarAsync<int>(
107: return await _connection.QueryAsync<DashboardCidsDTO>(query, userId, userId);
125: var savedConditions = await _connection.QueryAsync<ConditionsDTO>(query, userId, userId);
148: Quantity = await _connection.ExecuteScalarAsync<int>(
171: return await _connection.ExecuteScalarAsync<int>(
### UseCases\Services\DatabaseService.cs
33: await Connection.ExecuteScalarAsync<int>("PRAGMA secure_delete = ON");
60: return Connection.InsertAsync(user);
65: return Connection.UpdateAsync(user);
77: await Connection.ExecuteAsync($"UPDATE [{tableName}] SET UserId = ? WHERE UserId = 0", userId);
125: await Connection.ExecuteAsync("UPDATE Patient SET IsActive = 1 WHERE IsActive IS NULL");
126: await Connection.ExecuteAsync("UPDATE Patient SET StatusReason = '' WHERE StatusReason IS NULL");
142: await EnsureColumnAsync("Visits", nameof(Visits.UpdatedAt), "TEXT NULL");
148: await Connection.ExecuteAsync("UPDATE Visits SET VisitDate = Date WHERE VisitDate IS NULL OR VisitDate = ''");
149: await Connection.ExecuteAsync("UPDATE Visits SET CreatedAt = Date WHERE CreatedAt IS NULL OR CreatedAt = ''");
150: await Connection.ExecuteAsync(
152: UPDATE Visits
182: INSERT INTO PatientBolsaFamilia (UserId, PatientId, ResponsiblePatientId, NisNumber)
195: // is now authoritative; stale flags must not restore benefits removed in registration.
196: connection.Execute("UPDATE Patient SET BolsaFamilia = 0 WHERE COALESCE(BolsaFamilia, 0) = 1");
202: await Connection.ExecuteAsync(
204: INSERT INTO PatientInsulinDependency (UserId, PatientId)
221: await Connection.ExecuteAsync(
223: INSERT INTO PatientPregnancy
269: await Connection.ExecuteAsync(
271: DELETE FROM PatientVaccineDose
285: await Connection.ExecuteAsync("UPDATE PatientVaccineDose SET CreatedAt = COALESCE(AppliedAt, CURRENT_TIMESTAMP) WHERE CreatedAt IS NULL OR CreatedAt = ''");
286: await Connection.ExecuteAsync("UPDATE PatientVaccineDose SET AppliedAt = CreatedAt WHERE IsApplied = 1 AND (AppliedAt IS NULL OR AppliedAt = '')");
291: return Connection.ExecuteAsync(
293: INSERT INTO PatientVaccineDose (UserId, PatientId, DoseKey, IsApplied, AppliedAt, LotNumber, Notes, CreatedAt)
339: await Connection.UpdateAsync(user);
343: if (removedLegacySecrets) await Connection.ExecuteAsync("VACUUM");
353: await Connection.ExecuteAsync($"ALTER TABLE [{tableName}] ADD COLUMN {columnName} {definition}");
358: var columns = await Connection.QueryAsync<TableColumnInfo>($"PRAGMA table_info([{tableName}])");
364: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_Name ON Patient(Name COLLATE NOCASE)");
365: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_UserId_Name ON Patient(UserId, Name COLLATE NOCASE)");
366: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_UserId_IsActive ON Patient(UserId, IsActive)");
367: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_UserId_IsActive_Name ON Patient(UserId, IsActive, Name COLLATE NOCASE)");
368: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_SearchName ON Patient(SearchName)");
369: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_SearchMotherName ON Patient(SearchMotherName)");
370: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_SearchFatherName ON Patient(SearchFatherName)");
371: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_SusNumber ON Patient(SusNumber COLLATE NOCASE)");
372: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_FamilyResponsibleSus ON Patient(FamilyResponsibleSus COLLATE NOCASE)");
373: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_MotherPatientId ON Patient(MotherPatientId)");
374: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_FatherPatientId ON Patient(FatherPatientId)");
375: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_FamilyResponsiblePatientId ON Patient(FamilyResponsiblePatientId)");
376: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_HouseId ON Patient(HouseId)");
377: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_UserId_HouseId ON Patient(UserId, HouseId)");
378: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_Family_House ON Patient(FamilyId, HouseId)");
379: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Patient_UserId_Family_House ON Patient(UserId, FamilyId, HouseId)");
380: await Connection.ExecuteAsync("CREATE UNIQUE INDEX IF NOT EXISTS UX_PatientBolsaFamilia_User_Patient ON PatientBolsaFamilia(UserId, PatientId)");
381: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_PatientBolsaFamilia_User_Responsible ON PatientBolsaFamilia(UserId, ResponsiblePatientId)");
382: await Connection.ExecuteAsync("CREATE UNIQUE INDEX IF NOT EXISTS UX_PatientInsulinDependency_User_Patient ON PatientInsulinDependency(UserId, PatientId)");
383: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_PatientPregnancy_User_Patient ON PatientPregnancy(UserId, PatientId)");
384: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_PatientPregnancy_User_Status ON PatientPregnancy(UserId, Status)");
385: await Connection.ExecuteAsync("CREATE UNIQUE INDEX IF NOT EXISTS UX_CareNotification_User_Key ON CareNotification(UserId, UniqueKey)");
386: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_CareNotification_User_Status ON CareNotification(UserId, Status)");
387: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_CareNotification_User_Patient ON CareNotification(UserId, PatientId)");
388: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_PatientCID_UserId_PatientId ON PatientCID(UserId, PatientId)");
389: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_PatientCID_UserId_CidId ON PatientCID(UserId, CidId)");
390: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_PatientConditions_UserId_PatientId ON PatientConditions(UserId, PatientId)");
391: await Connection.ExecuteAsync("CREATE UNIQUE INDEX IF NOT EXISTS UX_PatientVaccineDose_User_Patient_Dose ON PatientVaccineDose(UserId, PatientId, DoseKey)");
392: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_PatientVaccineDose_UserId_PatientId ON PatientVaccineDose(UserId, PatientId)");
393: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_CepAddressCache_Cep ON CepAddressCache(Cep)");
394: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_House_Tipo_Rua_Numero ON House(TipoLogradouro COLLATE NOCASE, Rua COLLATE NOCASE, NumeroCasa)");
395: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_House_UserId_Tipo_Rua_Numero ON House(UserId, TipoLogradouro COLLATE NOCASE, Rua COLLATE NOCASE, NumeroCasa)");
396: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_House_Rua_Numero ON House(Rua COLLATE NOCASE, NumeroCasa)");
397: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_House_SearchRua_Numero ON House(SearchRua, NumeroCasa)");
398: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_House_UserId_SearchRua_Numero ON House(UserId, SearchRua, NumeroCasa)");
399: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_House_SearchComplemento ON House(SearchComplemento)");
400: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Note_UserId_CreationDate ON Note(UserId, CreationDate)");
401: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Visits_UserId_Date ON Visits(UserId, Date)");
402: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Visits_UserId_Patient_Date ON Visits(UserId, PatientId, VisitDate)");
403: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Visits_UserId_Status_Date ON Visits(UserId, Status, VisitDate)");
404: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_VisitCareLine_UserId_VisitId ON VisitCareLine(UserId, VisitId)");
405: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_VisitCareLine_UserId_Type ON VisitCareLine(UserId, CareLineType)");
406: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_VisitBatch_UserId_Family_Date ON VisitBatch(UserId, HouseId, FamilyId, VisitDate)");
407: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Family_UserId_IdFamilia ON Family(UserId, IdFamilia)");
408: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_CidSubcategory_Code ON CidSubcategory(Code COLLATE NOCASE)");
409: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_CidSubcategory_Description ON CidSubcategory(Description COLLATE NOCASE)");
410: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_CidCategory_Code ON CidCategory(Code COLLATE NOCASE)");
411: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_CidGroup_Code ON CidGroup(Code COLLATE NOCASE)");
412: await Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_CidChapter_Code ON CidChapter(Code COLLATE NOCASE)");
434: await Connection.UpdateAsync(patient);
451: await Connection.UpdateAsync(house);
466: await Connection.ExecuteAsync(
468: INSERT OR IGNORE INTO CepAddressCache
487: .Where(patient => patient.HouseId > 0 && patient.FamilyId > 0)
488: .GroupBy(patient => new { patient.HouseId, patient.FamilyId });
493: var memberSusNumbers = groupPatients
494: .SelectMany(patient => patient.SusNumbers)
500: .Where(sus => !string.IsNullOrWhiteSpace(sus) && memberSusNumbers.Contains(SusNumberSet.Parse(sus).FirstOrDefault() ?? string.Empty))
506: : groupPatients.FirstOrDefault(patient => patient.SusNumbers.Count > 0)?.PrimarySusNumber;
521: await Connection.UpdateAsync(patient);
### UseCases\Services\FamilyManager.cs
7: public async Task AddPeopleToFamily(List<int> ids, int houseId, int familyId, int responsiblePatientId)
20: if (responsible == null || string.IsNullOrWhiteSpace(responsible.SusNumber))
31: pessoa.FamilyId = familyId;
32: pessoa.HouseId = houseId;
33: pessoa.FamilyResponsibleSus = responsible.PrimarySusNumber;
34: await service.UpdatePatient(pessoa);
45: var oldHouseId = pessoa.HouseId;
46: var oldFamilyId = pessoa.FamilyId;
47: var wasResponsible = pessoa.HasSusNumber(pessoa.FamilyResponsibleSus);
49: pessoa.FamilyId = -1;
50: pessoa.HouseId = -1;
52: await service.UpdatePatient(pessoa);
54: if (wasResponsible && oldHouseId > 0 && oldFamilyId > 0)
56: await PromoteNewResponsibleAsync(oldHouseId, oldFamilyId);
61: private async Task PromoteNewResponsibleAsync(int houseId, int familyId)
63: var remainingPeople = await service.GetPatientsByFamilyAndHouseId(familyId, houseId) ?? [];
64: var newResponsible = remainingPeople.FirstOrDefault(patient => !string.IsNullOrWhiteSpace(patient.SusNumber));
73: person.FamilyResponsibleSus = newResponsible.PrimarySusNumber;
74: await service.UpdatePatient(person);
### UseCases\Services\FamilyService.cs
16: return await _connection.QueryAsync<Family>("SELECT * FROM Family WHERE UserId = ? ORDER BY IdFamilia", userId);
33: var result = await _connection.QueryAsync<Family>("SELECT * FROM Family WHERE IdFamilia = ? AND UserId = ?", id, userId);
43: public async Task<List<Patient>> GetPersonOfFamilyById(int houseId, int familyId)
45: if (houseId <= 0)
46: throw new ArgumentException("O ID da residência deve ser maior que zero.", nameof(houseId));
50: return await _connection.QueryAsync<Patient>(
51: "SELECT * FROM Patient WHERE HouseId = ? AND FamilyId = ? AND UserId = ?",
52: houseId, familyId, currentUserContext.RequireCurrentUserId()
70: return await _connection.ExecuteAsync(
71: "INSERT INTO Family (IdFamilia, UserId, IdPessoa) VALUES (?, ?, ?)",
90: return await _connection.ExecuteAsync("DELETE FROM Family WHERE IdFamilia = ? AND UserId = ?", id, userId);
107: await _connection.ExecuteAsync(
108: "UPDATE Family SET IdPessoa = ? WHERE IdFamilia = ? AND UserId = ?",
119: public async Task<int> GetMaxIdAsync(int houseId)
121: if (houseId <= 0)
122: throw new ArgumentException("O ID da residência deve ser maior que zero.", nameof(houseId));
126: var result = await _connection.ExecuteScalarAsync<int?>(
127: "SELECT MAX(FamilyId) FROM Patient WHERE HouseId = ? AND UserId = ?",
128: houseId,
135: Console.WriteLine($"Erro ao obter o maior IdFamilia para HouseId={houseId}: {ex.Message}");
### UseCases\Services\HouseImportService.cs
158: await houseService.UpdateHouseAsync(existingHouse);
159: result.UpdatedCount++;
### UseCases\Services\HouseService.cs
20: await repository.DeleteAsync(house);
80: await repository.InsertAsync(house);
87: await repository.UpdateAsync(house);
### UseCases\Services\ImportHistoryService.cs
12: await database.Connection.InsertAsync(history);
### UseCases\Services\NoteReminderService.cs
80: // SQLite still contains the prior reminder. Restore its OS counterpart.
### UseCases\Services\NoteService.cs
17: return await _database.QueryAsync<Note>("SELECT * FROM Note WHERE UserId = ? ORDER BY CreationDate", userId);
31: await _database.ExecuteAsync(
32: "INSERT INTO Note (UserId, Content, CreationDate, NotifyOn) VALUES (?, ?, ?, ?)",
50: await _database.ExecuteAsync(
51: "UPDATE Note SET Content = ?, CreationDate = ?, NotifyOn = ? WHERE Id = ? AND UserId = ?",
65: await _database.ExecuteAsync("DELETE FROM Note WHERE Id = ? AND UserId = ?", id, currentUserContext.RequireCurrentUserId());
### UseCases\Services\PatientCidService.cs
14: await _connection.InsertAsync(CID);
20: await _connection.UpdateAsync(CID);
26: await _connection.ExecuteAsync("DELETE FROM PatientCID WHERE Id = ? AND UserId = ?", id, userId);
32: await _connection.Table<PatientCID>().Where(c => c.PatientId == id && c.UserId == userId).DeleteAsync();
### UseCases\Services\PatientFamilyLinkResolver.cs
68: if (context.Patient.HouseId <= 0 && context.ImportedHouse is { } importedHouse &&
94: ShouldSetLink(context.Patient.HouseId, columnMap.OverwriteExistingFamilyLinks))
96: context.Patient.HouseId = matchedHouse.CasaId;
97: context.ResolvedHouseId = matchedHouse.CasaId;
98: await patientService.UpdatePatient(context.Patient);
100: else if (context.Patient.HouseId > 0)
102: context.ResolvedHouseId = context.Patient.HouseId;
105: if (context.RowNumber > 0 && context.Patient.HouseId <= 0 && context.ImportedHouse is { } pendingHouse &&
112: var nextFamilyIdByHouse = new Dictionary<int, int>();
120: await EnsureResponsibleFamilyAsync(context.Patient, columnMap, nextFamilyIdByHouse);
125: .Where(context => context.Patient.HouseId > 0)
126: .SelectMany(context => context.Patient.SusNumbers.Select(sus => new { Sus = sus, context.Patient }))
139: nextFamilyIdByHouse);
143: await patientService.UpdatePatient(context.Patient);
144: result.Details.Add($"{RowLabel(context)}: {context.Patient.Name}: família {context.Patient.FamilyId}, responsável #{context.Patient.FamilyResponsiblePatientId} associados.");
150: !currentResponsible.HasSusNumber(context.FamilyResponsibleSus))
152: if (context.IsFamilyResponsible && context.Patient.FamilyId <= 0)
189: await patientService.UpdatePatient(context.Patient);
206: Dictionary<int, int> nextFamilyIdByHouse)
213: responsible.FamilyResponsibleSus = responsible.PrimarySusNumber;
217: if (responsible.HouseId > 0 && ShouldSetLink(responsible.FamilyId, columnMap.OverwriteExistingFamilyLinks))
219: responsible.FamilyId = await GetNextFamilyIdAsync(responsible.HouseId, nextFamilyIdByHouse);
225: await patientService.UpdatePatient(responsible);
234: Dictionary<int, int> nextFamilyIdByHouse)
252: .Where(candidate => context.Patient.HouseId <= 0 || candidate.HouseId == context.Patient.HouseId)
260: .Where(candidate => context.Patient.HouseId <= 0 || candidate.HouseId == context.Patient.HouseId)
275: if (responsible.HouseId <= 0) return false;
276: if (context.Patient.HouseId <= 0) { context.Patient.HouseId = responsible.HouseId; changed = true; }
277: if (responsible.FamilyId <= 0)
279: responsible.FamilyId = await GetNextFamilyIdAsync(responsible.HouseId, nextFamilyIdByHouse);
281: responsible.FamilyResponsibleSus = responsible.PrimarySusNumber;
282: await patientService.UpdatePatient(responsible);
289: context.Patient.FamilyResponsibleSus = responsible.PrimarySusNumber;
293: if (responsible.FamilyId > 0 && context.Patient.FamilyId != responsible.FamilyId && ShouldSetLink(context.Patient.FamilyId, columnMap.OverwriteExistingFamilyLinks))
295: context.Patient.FamilyId = responsible.FamilyId;
327: private async Task<int> GetNextFamilyIdAsync(int houseId, Dictionary<int, int> nextFamilyIdByHouse)
329: if (!nextFamilyIdByHouse.TryGetValue(houseId, out var nextFamilyId))
331: nextFamilyId = await familyService.GetMaxIdAsync(houseId) + 1;
334: nextFamilyIdByHouse[houseId] = nextFamilyId + 1;
335: return nextFamilyId;
352: if (child.HouseId > 0 &&
353: indexes.PatientsByNameAndHouse.TryGetValue((normalizedName, child.HouseId), out var sameHouseCandidates))
435: .SelectMany(patient => patient.SusNumbers.Select(sus => new { Sus = sus, Patient = patient }))
444: .Where(patient => !string.IsNullOrWhiteSpace(patient.Name) && patient.HouseId > 0)
445: .GroupBy(patient => (Name: Normalize(patient.Name), patient.HouseId))
601: public Dictionary<(string Name, int HouseId), List<Patient>> PatientsByNameAndHouse { get; init; } = [];
622: public int? ResolvedHouseId { get; set; }
### UseCases\Services\PatientImportService.cs
92: .SelectMany(patient => patient.SusNumbers.Select(sus => new { Sus = sus, Patient = patient }))
123: var susNumber = GetCell(row, columns.SusIndex);
124: var susNumbers = SusNumberSet.Parse(susNumber);
141: foreach (var sus in susNumbers)
145: if (candidates.Count > 1 || (susNumbers.Count == 0 && !hasIdentity))
156: ? UpdateImportedPatientFields(patient, name, susNumber, motherName, fatherName, observation, importedSex, birthDate)
157: : SetImportedPatientFields(patient, name, susNumber, motherName, fatherName, observation, importedSex, birthDate);
196: await patientService.UpdatePatient(patient);
214: foreach (var oldSus in previous.SusNumbers)
221: foreach (var savedSus in patient.SusNumbers)
280: result.UpdatedCount++;
326: string susNumber,
334: patient.SusNumber = susNumber.Trim();
357: ("Nome", before.Name, patient.Name), ("CNS", before.SusNumber, patient.SusNumber),
367: private static bool UpdateImportedPatientFields(Patient patient, string name, string susNumber, string motherName,
372: patient.AddSusNumbers(susNumber);
389: SusIndex = FindColumnIndex(headerMap, columnMap.SusNumberColumn),
433: await conditionsRepository.DeleteConditionAsync(condition.Id);
440: await conditionsRepository.InsertConditionAsync(new PatientConditions
### UseCases\Services\PatientService.cs
12: await repository.DeleteAsync(Id);
52: await repository.InsertAsync(patient);
59: await repository.UpdateAsync(patient);
63: public Task<List<Patient>?> GetPatientsByHouseId(int houseId)
65: return repository.GetByHouseIdAsync(houseId);
68: public Task<List<Patient>?> GetPatientsByFamilyAndHouseId(int familyId, int houseId)
70: return repository.GetByFamilyAndHouseIdAsync(familyId, houseId);
### UseCases\Services\PregnancyService.cs
95: await _connection.UpdateAsync(pregnancy);
100: await _connection.InsertAsync(pregnancy);
126: await _connection.UpdateAsync(activePregnancy);
### UseCases\Services\UserDataCleanupService.cs
48: connection.Execute("DELETE FROM House WHERE UserId = ?", userId);
54: connection.Execute("DELETE FROM ImportHistory WHERE UserId = ?", userId);
55: connection.Execute("DELETE FROM PatientCID WHERE UserId = ?", userId);
56: connection.Execute("DELETE FROM PatientConditions WHERE UserId = ?", userId);
57: connection.Execute("DELETE FROM PatientBolsaFamilia WHERE UserId = ?", userId);
58: connection.Execute("DELETE FROM PatientInsulinDependency WHERE UserId = ?", userId);
59: connection.Execute("DELETE FROM PatientVaccineDose WHERE UserId = ?", userId);
60: connection.Execute("DELETE FROM Vaccines WHERE UserId = ?", userId);
61: connection.Execute("DELETE FROM VisitCareLine WHERE UserId = ?", userId);
62: connection.Execute("DELETE FROM VisitBatch WHERE UserId = ?", userId);
63: connection.Execute("DELETE FROM Visits WHERE UserId = ?", userId);
64: connection.Execute("DELETE FROM Family WHERE UserId = ?", userId);
65: connection.Execute("DELETE FROM Patient WHERE UserId = ?", userId);
70: connection.Execute("DELETE FROM VisitCareLine WHERE UserId = ?", userId);
71: connection.Execute("DELETE FROM VisitBatch WHERE UserId = ?", userId);
72: connection.Execute("DELETE FROM Visits WHERE UserId = ?", userId);
73: connection.Execute("DELETE FROM Family WHERE UserId = ?", userId);
74: connection.Execute("DELETE FROM House WHERE UserId = ?", userId);
77: UPDATE Patient
78: SET HouseId = -1,
79: FamilyId = -1,
89: connection.Execute("DELETE FROM Note WHERE UserId = ?", userId);
### UseCases\Services\VaccineService.cs
83: await _database.InsertAsync(new PatientVaccineDose
104: await _database.UpdateAsync(first);
108: await _database.DeleteAsync(duplicate);
123: await _database.DeleteAsync(existing);
### UseCases\Services\VisitsService.cs
66: var rows = await _connection.QueryAsync<VisitRecordRow>(
72: v.HouseId,
73: v.FamilyId,
77: COALESCE(NULLIF(TRIM(responsible.Name), ''), 'Familia ' || v.FamilyId) AS FamilyName
87: ORDER BY FamilyName COLLATE NOCASE, v.FamilyId, v.VisitDate DESC, PatientName COLLATE NOCASE
91: var careLineRows = await _connection.QueryAsync<VisitCareLineRow>(
111: .GroupBy(row => new { row.HouseId, row.FamilyId, row.FamilyName })
114: HouseId = group.Key.HouseId,
115: FamilyId = group.Key.FamilyId,
132: public async Task<List<VisitFamilyMemberOptionDto>> GetFamilyVisitOptionsAsync(int houseId, int familyId, DateTime visitDate)
138: patient.HouseId == houseId &&
139: patient.FamilyId == familyId)
196: HouseId = patient.HouseId,
197: FamilyId = patient.FamilyId,
247: await _connection.InsertAsync(visit);
264: if (request.HouseId <= 0 || request.FamilyId <= 0)
284: .Where(patient => patient.UserId == userId && patient.HouseId == request.HouseId && patient.FamilyId == request.FamilyId)
302: HouseId = request.HouseId,
303: FamilyId = request.FamilyId,
309: await _connection.InsertAsync(batch);
310: var address = await GetAddressAsync(userId, request.HouseId);
320: HouseId = request.HouseId,
321: FamilyId = request.FamilyId,
332: await _connection.InsertAsync(visit);
348: await _connection.ExecuteAsync("DELETE FROM VisitCareLine WHERE VisitId = ? AND UserId = ?", id, userId);
349: await _connection.ExecuteAsync("DELETE FROM Visits WHERE Id = ? AND UserId = ?", id, userId);
356: var deletedCareLines = await _connection.ExecuteAsync(
358: DELETE FROM VisitCareLine
371: var deletedVisits = await _connection.ExecuteAsync(
372: "DELETE FROM Visits WHERE UserId = ? AND VisitDate < ?",
376: var deletedBatches = await _connection.ExecuteAsync(
377: "DELETE FROM VisitBatch WHERE UserId = ? AND VisitDate < ?",
381: await _connection.ExecuteAsync(
383: DELETE FROM VisitCareLine
418: HouseId = first.HouseId,
419: FamilyId = first.FamilyId,
442: return _connection.ExecuteScalarAsync<int>(
449: return _connection.ExecuteScalarAsync<int>(
459: return _connection.ExecuteScalarAsync<int>(
480: var exists = await _connection.ExecuteScalarAsync<int>(
498: await _connection.InsertAsync(new VisitCareLine
610: var visits = await _connection.QueryAsync<VisitDateRow>(
635: return _connection.ExecuteScalarAsync<DateTime?>(
648: private async Task<string> GetAddressAsync(int userId, int houseId)
650: var house = await _connection.Table<House>().FirstOrDefaultAsync(item => item.UserId == userId && item.CasaId == houseId);
708: if (patient.HouseId <= 0 && patient.FamilyId <= 0)
713: if (patient.HouseId <= 0)
715: return $"Familia {patient.FamilyId} sem residência vinculada";
718: if (patient.FamilyId <= 0)
723: return $"Família {patient.FamilyId}";
748: public int HouseId { get; set; }
749: public int FamilyId { get; set; }
### ViewModels\AddFamilyViewModel.cs
75: int familyId = IsEdit ? IdPessoa : await _familyService.GetMaxIdAsync(IdHouse) + 1;
78: await _familyManager.AddPeopleToFamily(susList, IdHouse, familyId, ResponsiblePerson.Id);
141: ContainsTerm(n.SusNumber, normalizedTerm) ||
147: .Select(p => new Pessoa { Nome = p.Name, Id = p.Id, SusNumber = p.SusNumber })
167: var pessoaSelecionada = new Pessoa { Nome = pessoa.Name, Id = pessoa.Id, SusNumber = pessoa.SusNumber };
198: var registros = await _patientService.GetPatientsByFamilyAndHouseId(IdPessoa, IdHouse) ?? [];
204: Pessoas.Add(new Pessoa { Nome = r.Name, Id = r.Id, SusNumber = r.SusNumber });
212: SusNumberSet.Overlaps(p.SusNumber, responsibleSus)) ??
### ViewModels\AddHouseViewModel.cs
17: public async Task LoadHouseAsync(int houseId)
23: HouseModel = await _houseService.GetHouseByIdAsync(houseId) ?? new House();
55: await _houseService.UpdateHouseAsync(HouseModel);
### ViewModels\AddRegisterViewModel.cs
269: if (string.IsNullOrWhiteSpace(CurrentPatient?.Name) || string.IsNullOrWhiteSpace(CurrentPatient?.SusNumber))
277: var duplicate = allPatients.FirstOrDefault(p => p.Id != CurrentPatient.Id && p.SusNumbers.Intersect(CurrentPatient.SusNumbers).Any());
298: await _patientService.UpdatePatient(CurrentPatient);
308: await _patientCid.DeletePatientCIDByPatientId(CurrentPatient.Id);
309: await _conditionsRepository.DeleteConditionsByPatientIdAsync(CurrentPatient.Id);
320: await _conditionsRepository.InsertConditionAsync(new PatientConditions
405: await _bolsaFamiliaRepository.DeleteByPatientIdAsync(CurrentPatient.Id);
459: await _insulinDependencyRepository.DeleteByPatientIdAsync(CurrentPatient.Id);
693: patient.SusNumber ??= string.Empty;
### ViewModels\ConditionPopupViewModel.cs
94: // Ensure UI update runs on main thread
### ViewModels\DataCleanupViewModel.cs
32: await cleanupService.DeleteAsync(scope);
### ViewModels\FamiliesViewModel.cs
40: internal int HouseId => _idHouse;
109: var familiesFromDb = await _patientService.GetPatientsByHouseId(_idHouse);
111: .Where(p => p.FamilyId > 0)
112: .GroupBy(p => p.FamilyId)
174: var pessoasDaFamilia = await _patientService.GetPatientsByFamilyAndHouseId(idFamily, _idHouse);
178: person.HouseId = -1;
179: person.FamilyId = -1;
182: await _patientService.UpdatePatient(person);
### ViewModels\HousesPageViewModel.cs
175: var people = await _patientService.GetPatientsByHouseId(id);
181: person.HouseId = -1;
182: person.FamilyId = -1;
184: await _patientService.UpdatePatient(person);
188: await _houseService.DeleteHouseAsync(id);
201: await NavigateToAsync("addhouse", new Dictionary<string, object> { { "houseId", houseListItem.CasaId } });
### ViewModels\ImportDataViewModel.cs
70: PatientImportSummary = BuildSummary(result.ImportedCount, result.UpdatedCount, result.IgnoredCount) + $" | Mesclados: {result.MergedCount}. Consulte o histórico para detalhes.";
132: HouseImportSummary = BuildSummary(result.ImportedCount, result.UpdatedCount, result.IgnoredCount);
### ViewModels\NotesPageViewModel.cs
47: await _noteService.DeleteNoteAsync(id);
### ViewModels\OverallViewModel.cs
217: await RestoreCombinedMetricsAsync();
291: case OverallMetricMenuAction.RestoreHiddenMetrics:
292: await RestoreRemovedRootMetricAsync(targetDashboard, removedRootMetrics);
657: private async Task RestoreRemovedRootMetricAsync(
746: dashboard.Insert(firstCombinationIndex.Value, metric);
810: private async Task RestoreCombinedMetricsAsync()
### ViewModels\PersonsInfoViewModel.cs
106: var houseId = PersonInfo?.HouseId ?? 0;
107: if (houseId <= 0 || IsBusy)
115: await NavigateToAsync("families", new Dictionary<string, object> { { "id", houseId } });
### ViewModels\RegistersViewModel.cs
230: await _patientService.DeletePatient(id);
### ViewModels\VisitBatchPopupViewModel.cs
11: private readonly int houseId;
12: private readonly int familyId;
21: public VisitBatchPopupViewModel(int houseId, int familyId, IEnumerable<VisitFamilyMemberOptionDto> people)
23: this.houseId = houseId;
24: this.familyId = familyId;
61: HouseId = houseId,
62: FamilyId = familyId,
### ViewModels\VisitRecordsViewModel.cs
126: await visitsService.DeleteVisitAsync(id);
132: if (group is null || group.HouseId <= 0)
137: await NavigateToAsync("families", new Dictionary<string, object> { { "id", group.HouseId } });
### Views\AddFamilyPage.xaml
79: Command="{Binding Source={RelativeSource AncestorType={x:Type ContentPage}}, Path=BindingContext.DeleteCommand}"
### Views\AddHouse.xaml.cs
11: private int? _houseId;
28: if (query.TryGetValue("houseId", out var houseId))
30: _houseId = Convert.ToInt32(houseId);
40: if (!_loaded && _houseId is int houseId)
43: _ = viewModel.LoadHouseAsync(houseId);
### Views\AddRegister.xaml
157: Text="{Binding SusNumber, Mode=TwoWay}"/>
### Views\AllVisits.xaml
85: CommandParameter="{Binding HouseId}" />
### Views\AllVisits.xaml.cs
23: ScrollToTopButtonController.UpdateVisibility(BackToTopButton, e);
### Views\App.xaml.cs
35: _ = RestoreSessionAsync(window);
37: _ = RestoreSessionAsync(window);
83: private async Task RestoreSessionAsync(Window window)
### Views\BolsaFamiliaPage.xaml.cs
29: ScrollToTopButtonController.UpdateVisibility(BackToTopButton, e);
### Views\CIDView.xaml.cs
30: ScrollToTopButtonController.UpdateVisibility(BackToTopButton, e);
### Views\FamiliesPage.xaml
69: Command="{Binding Source={RelativeSource AncestorType={x:Type ContentPage}}, Path=BindingContext.DeleteFamilyCommand}"
### Views\FamiliesPage.xaml.cs
19: internal int HouseId => _viewModel?.HouseId ?? 0;
44: if (TryGetHouseId(query, out var houseId))
46: if (_viewModel?.HouseId == houseId)
52: houseId,
89: private static bool TryGetHouseId(IDictionary<string, object> query, out int houseId)
91: houseId = 0;
99: houseId = intId;
100: return houseId > 0;
103: return int.TryParse(id.ToString(), out houseId) && houseId > 0;
### Views\HousesPage.xaml
106: Command="{Binding Source={RelativeSource AncestorType={x:Type ContentPage}}, Path=BindingContext.DeleteCommand}"
### Views\HousesPage.xaml.cs
64: ScrollToTopButtonController.UpdateVisibility(BackToTopButton, e);
### Views\ImportHistoryPage.cs
35: var summary = new Label { Text = $"{entry.FileName}\nInício: {FormatDate(entry.StartedAt)}\nFim: {FormatDate(entry.FinishedAt)}\n{entry.Status}\nCriados: {report.ImportedCount} · Atualizados: {report.UpdatedCount} · Mesclados: {report.MergedCount} · Ignorados: {report.IgnoredCount}" };
### Views\InputFocusGuard.cs
17: _ = ClearTextInputFocusAfterNativeRestoreAsync(root);
21: private static async Task ClearTextInputFocusAfterNativeRestoreAsync(Element root)
48: #pragma warning restore CS0618
### Views\NotesPage.xaml
54: Command="{Binding Source={x:Reference NotesCollectionView}, Path=BindingContext.DeleteCommand}"
### Views\NotesPage.xaml.cs
30: ScrollToTopButtonController.UpdateVisibility(BackToTopButton, e);
### Views\OverallMetricMenuPopup.xaml
36: Clicked="RestoreHiddenMetricsButton_Clicked"/>
### Views\OverallMetricMenuPopup.xaml.cs
20: private async void RestoreHiddenMetricsButton_Clicked(object sender, EventArgs e)
22: await CloseAsync(OverallMetricMenuAction.RestoreHiddenMetrics);
### Views\ParentPatientPickerPopup.xaml
65: Text="{Binding SusNumber, StringFormat='CNS: {0}'}"
### Views\PersonsInfo.xaml
95: Text="{Binding SusNumber, Converter={converters:SUSFormatConverter}, StringFormat='CNS: {0}'}"
### Views\Registers.xaml
107: <Label Text="{Binding SusNumber, Converter={converters:SUSFormatConverter}, StringFormat='CNS: {0}'}" Style="{StaticResource BodyMuted}"/>
146: Command="{Binding Source={RelativeSource AncestorType={x:Type ContentPage}}, Path=BindingContext.DeleteCommand}"
### Views\Registers.xaml.cs
144: ScrollToTopButtonController.UpdateVisibility(BackToTopButton, e);
### Views\VisitPage.xaml.cs
11: public VisitPage(int houseId, int familyId, IEnumerable<VisitFamilyMemberOptionDto> people)
14: BindingContext = viewModel = new VisitBatchPopupViewModel(houseId, familyId, people);
### Views\VisitRecordsPage.xaml
125: Command="{Binding Source={RelativeSource AncestorType={x:Type ContentPage}}, Path=BindingContext.DeleteVisitCommand}"
### Views\VisitRecordsPage.xaml.cs
23: ScrollToTopButtonController.UpdateVisibility(BackToTopButton, e);
### Views\VisitSuggestionsPage.xaml
34: CommandParameter="{Binding HouseId}"/>
### Views\VisitSuggestionsPage.xaml.cs
23: ScrollToTopButtonController.UpdateVisibility(BackToTopButton, e);
### Tests\ACSView.AuthTests\ACSView.AuthTests.csproj
29: <Compile Include="../../Domain/ValueObjects/SusNumberSet.cs" />
### Tests\ACSView.AuthTests\Program.cs
18: await connection.ExecuteAsync("CREATE TABLE User (Id INTEGER PRIMARY KEY AUTOINCREMENT, Username TEXT, Password TEXT, SecurityQuestion TEXT, SecurityAnswer TEXT)");
19: await connection.ExecuteAsync("INSERT INTO User (Username, Password, SecurityQuestion, SecurityAnswer) VALUES (?, ?, ?, ?)", "antigo", "senha antiga", "Animal?", "gato");
21: await connection.ExecuteAsync("CREATE TABLE Note (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER, Content TEXT, CreationDate BIGINT, NotifyOn BIGINT)");
22: await connection.ExecuteAsync("INSERT INTO Note (UserId, Content, CreationDate, NotifyOn) VALUES (?, ?, ?, ?)", 1, "Nota de teste anterior à atualização", DateTime.Now, legacyReminderTime);
37: Check(await connection.ExecuteScalarAsync<int>("PRAGMA secure_delete") == 1, "Secure deletion enabled for SQLite migration");
51: await connection.InsertAsync(note);
86: await db.InsertUserAsync(v1);
93: // Recreate services and SQLite connection to prove the cooldown survives restart.
196: File.Delete(testPath);
### Tests\ACSView.BolsaFamiliaTests\ACSView.BolsaFamiliaTests.csproj
26: <Compile Include="../../Domain/ValueObjects/SusNumberSet.cs" />
### Tests\ACSView.BolsaFamiliaTests\Program.cs
22: await connection.ExecuteAsync("ALTER TABLE Patient ADD COLUMN BolsaFamilia INTEGER NOT NULL DEFAULT 0");
27: await connection.InsertAsync(removed);
28: await connection.InsertAsync(retained);
29: await connection.InsertAsync(otherUser);
31: await connection.ExecuteAsync("UPDATE Patient SET BolsaFamilia = 1");
33: await connection.InsertAsync(new PatientBolsaFamilia { UserId = 1, PatientId = removed.Id, ResponsiblePatientId = removed.Id });
36: await connection.InsertAsync(new PatientBolsaFamilia
51: await repository.DeleteByPatientIdAsync(removed.Id);
52: await repository.DeleteByPatientIdAsync(otherUser.Id);
62: Check(await repository.GetByPatientIdAsync(removed.Id) is null, "Restart does not recreate removed benefit");
89: File.Delete(path);
### Tests\ACSView.ImportTests\ACSView.ImportTests.csproj
34: <Compile Include="../../Domain/ValueObjects/SusNumberSet.cs" Link="Domain/ValueObjects/SusNumberSet.cs" />
### Tests\ACSView.ImportTests\Program.cs
50: var family = Proxy.For<IFamilyService>((m, a) => m == "GetMaxIdAsync" ? Task.FromResult(patients.Where(p => p.HouseId == (int)a[0]!).Select(p => p.FamilyId).DefaultIfEmpty(0).Max()) : throw new Exception(m));
70: Check(houses.Count == 1 && patients[0].HouseId == 1, "Residência criada por CEP e número");
76: Check(patients.Count == 1 && patients[0].SusNumber == "111" && second.MergedCount == 1, "CNS posterior encontra identidade sem duplicar");
82: var child = patients.Single(p => p.HasSusNumber("111"));
83: var parent = patients.Single(p => p.SusNumber == "222");
85: Check(child.FamilyResponsiblePatientId == parent.Id && child.FamilyId == parent.FamilyId && child.FamilyId > 0, "Responsável posterior calcula família sem duplicar residência");
93: Check(patients.Count == 2 && patients.Single(p => p.Id == child.Id).SusNumbers.SequenceEqual(["111", "999"]) && !conflict.Errors.Any(e => e.Contains("CNS divergente")), "Identidade igual acumula todos os CNS sem duplicar paciente");
97: Check(patients.Any(p => p.SusNumber == "333" && p.Name == "Carlos Souza"), "Alias vazio usa a outra coluna equivalente");
100: patients.Add(new Patient { Id = 90, Name = "Duplicado", MotherName = "Outra Mãe", BirthDate = new DateTime(1980, 1, 1), SusNumber = "888" });
101: patients.Add(new Patient { Id = 91, Name = "Duplicado", MotherName = "Outra Mãe", BirthDate = new DateTime(1980, 1, 1), SusNumber = "777" });
120: Check(patients.Single(p => p.HasSusNumber("111")).FatherPatientId == patients.Single(p => p.SusNumber == "444").Id, "Pai associado quando o nome está disponível");
127: Check(badDate.Errors.Any(e => e.Contains("data de nascimento inválida")) && patients.Single(p => p.HasSusNumber("111")).BirthDate == new DateTime(2010, 2, 1), "Data inválida é registrada e preserva nascimento anterior");
130: patients.Single(p => p.HasSusNumber("111")).StatusReason = "Informação manual";
131: patients.Single(p => p.HasSusNumber("111")).IsActive = false;
133: Check(patients.Single(p => p.HasSusNumber("111")).StatusReason == "Informação manual" && !patients.Single(p => p.HasSusNumber("111")).IsActive, "Campos de edição manual ausentes na planilha são preservados");
138: Check(patients.Count == originalCount && alternateSus.MergedCount == 1 && patients.Single(p => p.Id == child.Id).SusNumbers.SequenceEqual(["111", "999"]), "CNS secundário localiza cadastro sem dados de identidade e preserva os demais números");
140: Check(patients.Count == originalCount && patients.Single(p => p.Id == child.Id).SusNumbers.SequenceEqual(["111", "999", "555"]), "Vários CNS na célula são normalizados e deduplicados");
142: Check(sameSheet.MergedCount == 2 && patients.Count == originalCount && patients.Single(p => p.Id == child.Id).SusNumbers.SequenceEqual(["111", "999", "555", "666"]), "Novo CNS fica disponível para linhas seguintes da mesma importação");
144: Check(mixedPatients.IgnoredCount == 1 && patients.Single(p => p.Id == child.Id).SusNumbers.SequenceEqual(["111", "999", "555", "666"]), "CNS de pessoas diferentes exige revisão e não mistura cadastros");
147: var dependent = patients.Single(p => p.HasSusNumber("700"));
152: Check(updatedResponsible.SusNumbers.SequenceEqual(["222", "223"]) && dependent.FamilyResponsiblePatientId == parent.Id && dependent.FamilyId == updatedResponsible.FamilyId, "Responsável recebe CNS alternativo e resolve dependente importado antes");
156: Check(patients.Single(p => p.HasSusNumber("701")).FamilyResponsiblePatientId == parent.Id, "Novo dependente encontra responsável já cadastrado pelo CNS secundário");
158: var legacyPatient = new Patient { SusNumber = "123.4567.8901.2345" };
159: Check(legacyPatient.SusNumbers.SequenceEqual(["123456789012345"]), "Cadastro legado com único CNS continua compatível");
160: legacyPatient.SusNumbers = ["123456789012345", "987.6543.2109.8765", "123456789012345"];
162: Check(roundtripPatient.SusNumbers.SequenceEqual(["123456789012345", "987654321098765"]) && roundtripPatient.PrimarySusNumber == "123456789012345", "Coleção deduplicada mantém dados no ciclo de serialização");
166: database.Execute("CREATE TABLE Patient (Id INTEGER PRIMARY KEY AUTOINCREMENT, SusNumber TEXT)");
167: database.Execute("INSERT INTO Patient (SusNumber) VALUES (?)", "123.4567.8901.2345");
170: Check(persisted.SusNumbers.SequenceEqual(["123456789012345"]), "SQLite existente carrega CNS legado sem perda após atualização do modelo");
171: persisted.AddSusNumbers("987654321098765; 987.6543.2109.8765");
172: database.Update(persisted);
174: Check(reloaded.SusNumbers.SequenceEqual(["123456789012345", "987654321098765"]), "SQLite persiste e recarrega coleção sem duplicação");
175: Check(database.ExecuteScalar<int>("SELECT COUNT(*) FROM Patient WHERE SusNumber LIKE ?", "%987654321098765%") == 1, "Busca SQLite encontra CNS secundário na coluna compatível");
### Tests\ACSView.ReminderTests\Program.cs
60: Check(store.Notes[0].NotifyOn == now.AddHours(1) && scheduler.Pending[1].Message == "old", "Database failure restores previous request");
63: Check(scheduler.Pending[1].Message == "old", "Scheduler exception restores previous request");
66: Check(store.Notes[0].NotifyOn == now.AddHours(1), "Cancellation failure restores persisted intent");
### Tests\ACSView.Tests\ACSView.Tests.csproj
22: <Compile Include="..\..\Domain\ValueObjects\SusNumberSet.cs" Link="Domain\ValueObjects\SusNumberSet.cs" />
### Tests\ACSView.Tests\VisitRecordGroupingTests.cs
15: new() { HouseId = 1, FamilyId = 10, FamilyName = "Família Silva", Visits = [morning, previous] },
16: new() { HouseId = 1, FamilyId = 10, FamilyName = "Silva", Visits = [afternoon] },
17: new() { HouseId = 1, FamilyId = 11, FamilyName = "Família Silva", Visits = [otherFamily] },
18: new() { HouseId = 2, FamilyId = 10, FamilyName = "Família Silva", Visits = [otherHouse] }
23: var family = groups[0].Families.Single(item => item.HouseId == 1 && item.FamilyId == 10);
## Riscos confirmados antes de edição
- DatabaseService: sem versão persistente; alterações autocommit; deduplicação destrutiva de PatientVaccineDose; datas de vacina inventadas; backfill de responsável agrupa sem UserId e escolhe primeiro CNS; claim UserId=0 indiscriminado.
- VisitsService: leituras purgam automaticamente histórico maior que um ano e referências ausentes. Date é data legada; novas escritas atribuem Date e VisitDate à data do evento. Address é snapshot.
- Patient.SusNumber: setter remove texto, formatação e duplicatas em qualquer materialização seguida de UPDATE.
- Family.IdFamilia é PK global antiga; IdPessoa sem contrato operacional demonstrável. UI usa (UserId,HouseId,Patient.FamilyId), numerando por casa; não usa FamilyService.SaveFamily.
- PatientConditions.ForeignKey é DataAnnotations, não constraint SQLite. Não há PRAGMA foreign_keys na conexão DI health_app.db em FileSystem.AppDataDirectory.
- Exclusão individual apaga histórico clínico; limpeza de pacientes omite gravidez/notificações. Updates de Patient/House via ORM usam somente PK e podem reatribuir proprietário.
- Não existe serviço de backup/restauração/exportação de banco no código examinado; importações usam serviços e repositórios.
- Sentinelas: HouseId e FamilyId -1/0, PatientId=0 nas visitas legadas e UserId=0 sem proprietário determinado.
- Clínicos e catálogos permanecem separados; IDs de CID e descrições existentes devem permanecer. CommonConditions é modelo de UI sem PK.
