using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;
using SQLite;
using ACS_View.Infrastructure.Data.Migrations;

namespace ACS_View.UseCases.Services
{
    internal class VaccineService(IDatabaseService dbService, ICurrentUserContext currentUserContext) : IVaccineService
    {
        private readonly SQLiteAsyncConnection _database = dbService.Connection;

        public async Task<PatientVaccineScheduleDto?> GetScheduleForPatientAsync(int patientId)
        {
            var userId = currentUserContext.RequireCurrentUserId();
            var patient = await _database.Table<Patient>()
                .FirstOrDefaultAsync(patient => patient.Id == patientId && patient.UserId == userId);

            if (patient is null)
            {
                return null;
            }

            var applicationRows = await _database.QueryAsync<PatientVaccineDose>(
                $"SELECT {LegacyDateSql.Projection<PatientVaccineDose>()} FROM PatientVaccineDose WHERE UserId=? AND PatientId=? AND IsApplied=1",
                userId, patientId);

            var applicationByDose = applicationRows
                .OrderBy(dose => dose.Id)
                .GroupBy(dose => dose.DoseKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            var pregnancies = await _database.QueryAsync<PatientPregnancy>(
                $"SELECT {LegacyDateSql.Projection<PatientPregnancy>()} FROM PatientPregnancy WHERE UserId=? AND PatientId=? AND Status=?",
                userId, patientId, PregnancyStatus.Active);
            var pregnancy = pregnancies.OrderByDescending(item => item.Id).FirstOrDefault();
            var isPregnant = pregnancy is not null || await IsPregnantAsync(patientId, userId);
            var ageMonths = GetMonth(patient.BirthDate);
            var definitions = VaccineDoseCatalog
                .GetExpectedDefinitions(ageMonths, isPregnant)
                .Where(definition => !isPregnant || definition.Section == VaccineSectionType.Pregnant)
                .Where(definition => definition.DoseKey != "Influenza-Primaria2" || NeedsFirstInfluenzaSecondDose(applicationRows))
                .Select(definition => ForCurrentCycle(definition, pregnancy, applicationRows))
                .Where(definition => !CoveredByLegacyCombination(definition, applicationByDose))
                .ToList();
            // Retired products and previous age/season/pregnancy records remain visible under their original keys.
            var displayedKeys = definitions.Select(definition => definition.DoseKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
            definitions.AddRange(applicationRows.Where(row => displayedKeys.Add(row.DoseKey)).Select(row =>
                VaccineDoseCatalog.GetDefinition(row.DoseKey) ?? new VaccineDoseDefinition(
                    row.DoseKey, "Vacina histórica", row.DoseKey, VaccineSectionType.Adult, null, null,
                    IsRoutine: false, Guidance: "Registro preservado; produto não identificado no catálogo atual.")));
            var doses = definitions
                .Select(definition => BuildDoseDto(patient.BirthDate, definition, applicationByDose))
                .ToList();

            return new PatientVaccineScheduleDto
            {
                PatientId = patientId,
                BirthDate = patient.BirthDate,
                IsPregnant = isPregnant,
                ApplicationsByDose = doses
                    .Where(dose => dose.IsApplied)
                    .ToDictionary(dose => dose.Definition.DoseKey, StringComparer.OrdinalIgnoreCase),
                Doses = doses
            };
        }

        private static bool CoveredByLegacyCombination(VaccineDoseDefinition definition, IReadOnlyDictionary<string, PatientVaccineDose> applications)
        {
            if (definition.DoseKey == VaccineDoseKeys.HpvInfantil && applications.ContainsKey(VaccineDoseKeys.HpvAdolescente))
                return true;
            // Tetraviral documents both components. Never invent two new application rows/dates.
            return definition.DoseKey is "TripliceViral-Infantil2" or "Varicela-Infantil1" &&
                applications.ContainsKey(VaccineDoseKeys.TetraViral);
        }

        private static bool NeedsFirstInfluenzaSecondDose(IReadOnlyList<PatientVaccineDose> applications)
        {
            var influenza = applications.Where(row => row.DoseKey.StartsWith("Influenza-", StringComparison.OrdinalIgnoreCase)).ToList();
            return influenza.Any(row => row.DoseKey == $"Influenza-Child@season-{DateTime.Today.Year}") &&
                !influenza.Any(row => row.AppliedAt is DateTime date && date.Year < DateTime.Today.Year ||
                    row.DoseKey.Contains("@season-", StringComparison.Ordinal) &&
                    int.TryParse(row.DoseKey.Split("@season-")[1], out var season) && season < DateTime.Today.Year);
        }

        private static VaccineDoseDefinition ForCurrentCycle(
            VaccineDoseDefinition definition, PatientPregnancy? pregnancy, IReadOnlyList<PatientVaccineDose> applications)
        {
            if (definition.IsSeasonal)
            {
                var first = definition.DoseKey == "Influenza-Primaria2"
                    ? applications.FirstOrDefault(row => row.DoseKey == $"Influenza-Child@season-{DateTime.Today.Year}") : null;
                return definition with
                {
                    DoseKey = GetSeasonDoseKey(definition, applications),
                    DoseLabel = $"{definition.DoseLabel} · {DateTime.Today.Year}",
                    ContextRecommendedDate = first?.AppliedAt?.Date.AddDays(30)
                };
            }
            if (definition.PerPregnancy && pregnancy is not null)
            {
                var guidance = definition.Guidance;
                if (definition.PregnancyWeek is int week)
                    guidance += pregnancy.LastMenstrualPeriod is DateTime lmp
                        ? $" Nesta gestação, {week}ª semana a partir de {lmp.Date.AddDays(week * 7):dd/MM/yyyy}; confirmar avaliação clínica."
                        : " Idade gestacional não documentada: confirmar com a equipe antes de aprazar.";
                return definition with
                {
                    DoseKey = $"{definition.DoseKey}@pregnancy-{pregnancy.Id}", Guidance = guidance,
                    ContextRecommendedDate = definition.PregnancyWeek is int targetWeek
                        ? pregnancy.LastMenstrualPeriod?.Date.AddDays(targetWeek * 7) : null
                };
            }
            if (definition.RepeatIntervalMonths.HasValue ||
                definition.DoseKey.StartsWith("Covid-Periodica-", StringComparison.OrdinalIgnoreCase) && !definition.PerPregnancy)
            {
                var latest = applications.Where(row => row.DoseKey.Equals(definition.DoseKey, StringComparison.OrdinalIgnoreCase) ||
                        row.DoseKey.StartsWith(definition.DoseKey + "@", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(row => row.AppliedAt).ThenByDescending(row => row.Id).FirstOrDefault();
                if (latest is not null)
                    return definition with
                    {
                        DoseKey = $"{definition.DoseKey}@after-{latest.Id}",
                        ContextRecommendedDate = definition.RepeatIntervalMonths is int interval ? latest.AppliedAt?.Date.AddMonths(interval) : null,
                        Guidance = definition.Guidance + (latest.AppliedAt is DateTime applied && definition.RepeatIntervalMonths.HasValue
                            ? $" Última dose desta sequência: {applied:dd/MM/yyyy}; próximo intervalo de {definition.RepeatIntervalMonths} meses em {applied.AddMonths(definition.RepeatIntervalMonths.Value):dd/MM/yyyy}. Conferir outras doses documentadas."
                            : latest.AppliedAt.HasValue ? " Conferir o intervalo da próxima dose conforme o grupo e o histórico." : " Última aplicação sem data: confirmar a data para calcular o intervalo.")
                    };
            }
            return definition;
        }

        private static string GetSeasonDoseKey(VaccineDoseDefinition definition, IReadOnlyList<PatientVaccineDose> applications)
        {
            var key = definition.DoseKey;
            var year = DateTime.Today.Year;
            var matching = applications.Where(row => (row.DoseKey.Equals(key, StringComparison.OrdinalIgnoreCase) ||
                    row.DoseKey.StartsWith(key + "@", StringComparison.OrdinalIgnoreCase)) && row.AppliedAt?.Year == year)
                .OrderByDescending(row => row.AppliedAt).ThenByDescending(row => row.Id).FirstOrDefault();
            if (matching is not null) return matching.DoseKey;
            var seasonKey = $"{key}@season-{year}";
            // Old versions could put a previous year's application in the current season's slot.
            var occupied = applications.FirstOrDefault(row => row.DoseKey.Equals(seasonKey, StringComparison.OrdinalIgnoreCase));
            return occupied?.AppliedAt is DateTime date && date.Year != year ? $"{key}@after-{occupied.Id}" : seasonKey;
        }

        public async Task ApplyDoseAsync(VaccineApplicationRequestDto request)
        {
            var userId = currentUserContext.RequireCurrentUserId();

            var definition = VaccineDoseCatalog.GetDefinition(request.DoseKey);
            if (definition is null || request.IsNewPeriodicApplication && !definition.AllowsPeriodicApplications)
            {
                throw new ArgumentException("Dose de vacina invalida.", nameof(request));
            }

            if (request.ApplicationDate.HasValue && request.ApplicationDate.Value.Date > DateTime.Today)
            {
                throw new InvalidOperationException("A data de aplicacao nao pode ser futura.");
            }

            await _database.RunInTransactionAsync(connection =>
            {
                var patientExists = connection.Table<Patient>()
                    .Where(patient => patient.Id == request.PatientId && patient.UserId == userId)
                    .Count() > 0;
                if (!patientExists)
                {
                    throw new InvalidOperationException("Paciente nao encontrado.");
                }

                var saveRequest = request;
                var baseKey = request.DoseKey.Split('@')[0];
                var existingRows = request.IsNewPeriodicApplication
                    ? new List<PatientVaccineDose>()
                    : ReadDoseApplications(connection, userId, request.PatientId, request.DoseKey);
                if (request.IsNewPeriodicApplication)
                    saveRequest = request with { DoseKey = $"{baseKey}@application-{Guid.NewGuid():N}" };
                else if (existingRows.Count == 0 && definition.IsSeasonal && request.ApplicationDate is DateTime date)
                {
                    saveRequest = request with { DoseKey = $"{baseKey}@season-{date.Year}" };
                    var targetRows = ReadDoseApplications(connection, userId, request.PatientId, saveRequest.DoseKey);
                    if (targetRows.Any(row => row.IsApplied))
                    {
                        if (targetRows.All(row => row.AppliedAt.HasValue && row.AppliedAt.Value.Year != date.Year))
                            saveRequest = request with { DoseKey = $"{baseKey}@application-{Guid.NewGuid():N}" };
                        else
                            throw new InvalidOperationException("Já existe uma aplicação nesta temporada. Use Registrar nova dose ou edite o registro anterior.");
                    }
                    else if (targetRows.Count > 0) existingRows = targetRows;
                }
                if (existingRows.Count > 1)
                {
                    throw new InvalidOperationException("Há aplicações históricas distintas desta dose. Concilie os registros antes de editar.");
                }

                SaveApplication(connection, userId, saveRequest, existingRows.SingleOrDefault());
            });
            DataChangeTracker.MarkPatientsChanged();
        }

        public async Task ApplyOverdueDosesAsync(
            int patientId, int expectedUserId, IReadOnlyDictionary<string, DateTime> applicationDates)
        {
            var today = DateTime.Today;
            if (applicationDates.Count == 0 ||
                applicationDates.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase).Count != applicationDates.Count)
            {
                throw new ArgumentException("Informe uma data para cada dose atrasada.", nameof(applicationDates));
            }

            await _database.RunInTransactionAsync(connection =>
            {
                if (!currentUserContext.HasCurrentUser || currentUserContext.CurrentUserId != expectedUserId)
                {
                    throw new InvalidOperationException("O perfil ativo mudou. Abra o cartão vacinal novamente.");
                }

                var patient = connection.Table<Patient>()
                    .FirstOrDefault(patient => patient.Id == patientId && patient.UserId == expectedUserId)
                    ?? throw new InvalidOperationException("Paciente não encontrado.");
                var conditions = connection.Table<PatientConditions>()
                    .Where(condition => condition.UserId == expectedUserId && condition.PatientId == patientId)
                    .ToList();
                var isPregnant = conditions.Any(IsPregnancy) || connection.Table<PatientPregnancy>()
                    .Where(item => item.UserId == expectedUserId && item.PatientId == patientId && item.Status == PregnancyStatus.Active).Count() > 0;
                var definitions = VaccineDoseCatalog.GetExpectedDefinitions(GetMonth(patient.BirthDate), isPregnant)
                    .Where(definition => !isPregnant || definition.Section == VaccineSectionType.Pregnant)
                    .ToDictionary(definition => definition.DoseKey, StringComparer.OrdinalIgnoreCase);

                foreach (var (doseKey, date) in applicationDates)
                {
                    if (date.Date > today)
                    {
                        throw new InvalidOperationException("A data de aplicação não pode ser futura.");
                    }

                    var existingRows = ReadDoseApplications(connection, expectedUserId, patientId, doseKey);
                    if (!definitions.TryGetValue(doseKey, out var definition) ||
                        VaccineStatusCalculator.Calculate(patient.BirthDate, today, definition, null) != VaccineStatus.Late ||
                        existingRows.Count > 1 || existingRows.Any(row => row.IsApplied) ||
                        (doseKey is "TripliceViral-Infantil2" or "Varicela-Infantil1" &&
                            ReadDoseApplications(connection, expectedUserId, patientId, VaccineDoseKeys.TetraViral).Any(row => row.IsApplied)))
                    {
                        throw new InvalidOperationException("O cartão vacinal mudou ou contém registros conflitantes. Feche o lote e confira as doses.");
                    }

                    var existing = existingRows.SingleOrDefault();
                    ValidateBatchDate(connection, expectedUserId, patient, definition, date, applicationDates);
                    var request = new VaccineApplicationRequestDto(
                        patientId, definition.DoseKey, date, existing?.LotNumber ?? string.Empty, existing?.Notes ?? string.Empty);
                    SaveApplication(connection, expectedUserId, request, existing);
                }
            });

            DataChangeTracker.MarkPatientsChanged();
        }

        private static void ValidateBatchDate(SQLiteConnection connection, int userId, Patient patient,
            VaccineDoseDefinition definition, DateTime date, IReadOnlyDictionary<string, DateTime> batch)
        {
            if (date.Date < VaccineStatusCalculator.GetRecommendedDate(patient.BirthDate, definition))
                throw new InvalidOperationException("A data do lote é anterior à idade recomendada. Confira a dose individualmente.");
            if (definition.PreviousDoseKey is string previousKey)
            {
                var previous = ReadDoseApplications(connection, userId, patient.Id, previousKey).Where(row => row.IsApplied).ToList();
                DateTime? previousDate = previous.Count == 1 ? previous[0].AppliedAt : null;
                if (previous.Count == 0 && batch.TryGetValue(previousKey, out var batchDate)) previousDate = batchDate;
                if (!previousDate.HasValue || date.Date < previousDate.Value.Date.AddMonths(definition.MinimumIntervalMonths).AddDays(definition.MinimumIntervalDays))
                    throw new InvalidOperationException("Intervalo mínimo ou data da dose anterior não comprovados. Confira o esquema antes do registro em lote.");
            }
            if (definition.DoseKey == VaccineDoseKeys.Penta3)
            {
                var first = ReadDoseApplications(connection, userId, patient.Id, VaccineDoseKeys.Penta1).Where(row => row.IsApplied).ToList();
                DateTime? firstDate = first.Count == 1 ? first[0].AppliedAt : null;
                if (first.Count == 0 && batch.TryGetValue(VaccineDoseKeys.Penta1, out var batchDate)) firstDate = batchDate;
                if (!firstDate.HasValue || date.Date < firstDate.Value.Date.AddMonths(4))
                    throw new InvalidOperationException("Penta: conferir o intervalo mínimo de 4 meses entre a primeira e a terceira dose.");
            }
        }

        private static List<PatientVaccineDose> ReadDoseApplications(SQLiteConnection connection, int userId, int patientId, string doseKey)
        {
            return connection.Query<PatientVaccineDose>(
                $"SELECT {LegacyDateSql.Projection<PatientVaccineDose>()} FROM PatientVaccineDose WHERE UserId=? AND PatientId=? AND DoseKey=? COLLATE NOCASE",
                userId, patientId, doseKey);
        }

        private static void SaveApplication(SQLiteConnection connection, int userId, VaccineApplicationRequestDto request, PatientVaccineDose? existing)
        {
            if (existing is null)
            {
                connection.Insert(new PatientVaccineDose
                {
                    UserId = userId,
                    PatientId = request.PatientId,
                    DoseKey = request.DoseKey,
                    IsApplied = true,
                    AppliedAt = request.ApplicationDate?.Date,
                    LotNumber = request.LotNumber.Trim(),
                    Notes = request.Notes.Trim(),
                    CreatedAt = DateTime.Now
                });
                return;
            }

            connection.Execute("UPDATE PatientVaccineDose SET IsApplied=1,AppliedAt=?,LotNumber=?,Notes=? WHERE Id=? AND UserId=?",
                request.ApplicationDate?.Date, request.LotNumber.Trim(), request.Notes.Trim(), existing.Id, userId);
        }

        public async Task RemoveDoseApplicationAsync(int patientId, string doseKey)
        {
            var userId = currentUserContext.RequireCurrentUserId();
            var removed = false;
            await _database.RunInTransactionAsync(connection =>
            {
                var existingRows = ReadDoseApplications(connection, userId, patientId, doseKey);

                if (existingRows.Count > 1)
                {
                    throw new InvalidOperationException("Há aplicações históricas distintas desta dose. Concilie os registros antes de remover.");
                }

                if (existingRows.SingleOrDefault() is { } existing)
                {
                    removed = connection.Delete(existing) > 0;
                }
            });

            if (removed)
            {
                DataChangeTracker.MarkPatientsChanged();
            }
        }

        private async Task<bool> IsPregnantAsync(int patientId, int userId)
        {
            var conditions = await _database.Table<PatientConditions>()
                .Where(condition => condition.UserId == userId && condition.PatientId == patientId)
                .ToListAsync();

            return conditions.Any(IsPregnancy);
        }

        private static bool IsPregnancy(PatientConditions condition) =>
            string.Equals(HealthConditionCatalog.GetKey(condition.Description), HealthConditionCatalog.Gestante,
                StringComparison.OrdinalIgnoreCase);

        private static int GetMonth(DateTime birthDate)
        {
            var today = DateTime.Today;
            var months = (today.Year - birthDate.Year) * 12 + today.Month - birthDate.Month;

            if (today.Day < birthDate.Day)
            {
                months--;
            }

            return Math.Max(months, 0);
        }

        private static PatientVaccineDoseDto BuildDoseDto(
            DateTime birthDate,
            VaccineDoseDefinition definition,
            IReadOnlyDictionary<string, PatientVaccineDose> applicationByDose)
        {
            applicationByDose.TryGetValue(definition.DoseKey, out var application);
            var applicationDate = application?.AppliedAt?.Date;
            var recommendedDate = VaccineStatusCalculator.GetRecommendedDate(birthDate, definition);

            var status = VaccineStatusCalculator.Calculate(
                birthDate,
                DateTime.Today,
                definition,
                applicationDate);

            if (application is null && status is VaccineStatus.Due or VaccineStatus.Late && definition.PreviousDoseKey is string previousKey)
            {
                if (!applicationByDose.TryGetValue(previousKey, out var previous) || !previous.AppliedAt.HasValue ||
                    DateTime.Today < previous.AppliedAt.Value.Date.AddMonths(definition.MinimumIntervalMonths).AddDays(definition.MinimumIntervalDays))
                    status = VaccineStatus.RequiresAssessment;
                else
                {
                    var intervalDate = previous.AppliedAt.Value.Date.AddMonths(definition.MinimumIntervalMonths).AddDays(definition.MinimumIntervalDays);
                    if (intervalDate > recommendedDate) recommendedDate = intervalDate;
                }
            }

            if (application is { IsApplied: true } && !applicationDate.HasValue)
            {
                status = VaccineStatus.Applied;
            }

            return new PatientVaccineDoseDto(
                definition,
                application?.Id,
                recommendedDate,
                VaccineStatusCalculator.GetMaximumDate(birthDate, definition),
                applicationDate,
                application?.LotNumber ?? string.Empty,
                application?.Notes ?? string.Empty,
                status);
        }
    }
}
