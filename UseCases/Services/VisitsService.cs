using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;
using SQLite;

namespace ACS_View.UseCases.Services;

internal class VisitsService(IDatabaseService db, ICurrentUserContext currentUserContext) : IVisitsService
{
    private readonly SQLiteAsyncConnection _connection = db.Connection;

    public async Task<List<Visits>> GetAllVisitsAsync()
    {
        var userId = currentUserContext.RequireCurrentUserId();
        return await _connection
            .Table<Visits>()
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.VisitDate)
            .ToListAsync();
    }

    public async Task<VisitMonthlyOverviewDto> GetMonthlyOverviewAsync(DateTime month)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        await PurgeExpiredVisitsAsync(userId, DateTime.Today);
        var start = new DateTime(month.Year, month.Month, 1);
        var end = start.AddMonths(1);

        return new VisitMonthlyOverviewDto
        {
            TotalVisits = await CountVisitsAsync(userId, start, end, null),
            CompletedVisits = await CountVisitsAsync(userId, start, end, VisitStatus.Completed),
            AbsentVisits = await CountVisitsAsync(userId, start, end, VisitStatus.Absent),
            RefusedVisits = await CountVisitsAsync(userId, start, end, VisitStatus.Refused),
            ChildVisits = await CountCareLineVisitsAsync(userId, start, end, VisitCareLineType.Child),
            PregnancyPostpartumVisits =
                await CountCareLineVisitsAsync(userId, start, end, VisitCareLineType.Pregnancy) +
                await CountCareLineVisitsAsync(userId, start, end, VisitCareLineType.Postpartum),
            HypertensionVisits = await CountCareLineVisitsAsync(userId, start, end, VisitCareLineType.Hypertension),
            DiabetesVisits = await CountCareLineVisitsAsync(userId, start, end, VisitCareLineType.Diabetes),
            ElderlyVisits = await CountCareLineVisitsAsync(userId, start, end, VisitCareLineType.Elderly),
            BenefitVisits =
                await CountCareLineVisitsAsync(userId, start, end, VisitCareLineType.BolsaFamilia) +
                await CountCareLineVisitsAsync(userId, start, end, VisitCareLineType.Bpc)
        };
    }

    public async Task<List<VisitRecordFamilyGroupDto>> GetVisitRecordGroupsAsync(DateTime month, VisitStatus? status)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        await PurgeExpiredVisitsAsync(userId, DateTime.Today);
        var start = new DateTime(month.Year, month.Month, 1);
        var end = start.AddMonths(1);
        var parameters = new List<object> { userId, start, end };
        var statusClause = string.Empty;

        if (status.HasValue)
        {
            statusClause = "AND v.Status = ?";
            parameters.Add(status.Value.ToString());
        }

        var rows = await _connection.QueryAsync<VisitRecordRow>(
            $"""
            SELECT
                v.Id,
                v.PatientId,
                COALESCE(NULLIF(TRIM(p.Name), ''), 'Paciente legado') AS PatientName,
                v.HouseId,
                v.FamilyId,
                v.VisitDate,
                v.Status,
                v.Notes,
                COALESCE(NULLIF(TRIM(responsible.Name), ''), 'Familia ' || v.FamilyId) AS FamilyName
            FROM Visits v
            LEFT JOIN Patient p ON p.UserId = v.UserId AND p.Id = v.PatientId
            LEFT JOIN Patient responsible
                ON responsible.UserId = p.UserId
               AND responsible.Id = p.FamilyResponsiblePatientId
            WHERE v.UserId = ?
              AND v.VisitDate >= ?
              AND v.VisitDate < ?
              {statusClause}
            ORDER BY FamilyName COLLATE NOCASE, v.FamilyId, v.VisitDate DESC, PatientName COLLATE NOCASE
            """,
            [.. parameters]);

        var careLineRows = await _connection.QueryAsync<VisitCareLineRow>(
            """
            SELECT vc.VisitId, vc.CareLineType
            FROM VisitCareLine vc
            INNER JOIN Visits v ON v.UserId = vc.UserId AND v.Id = vc.VisitId
            WHERE vc.UserId = ?
              AND v.VisitDate >= ?
              AND v.VisitDate < ?
            """,
            userId,
            start,
            end);

        var careLinesByVisit = careLineRows
            .GroupBy(row => row.VisitId)
            .ToDictionary(
                group => group.Key,
                group => string.Join(", ", group.Select(row => GetCareLineLabel(row.CareLineType)).Where(label => !string.IsNullOrWhiteSpace(label)).Distinct()));

        return rows
            .GroupBy(row => new { row.HouseId, row.FamilyId, row.FamilyName })
            .Select(group => new VisitRecordFamilyGroupDto
            {
                HouseId = group.Key.HouseId,
                FamilyId = group.Key.FamilyId,
                FamilyName = group.Key.FamilyName,
                Visits = group.Select(row => new VisitRecordDto
                {
                    Id = row.Id,
                    PatientId = row.PatientId,
                    PatientName = row.PatientName,
                    VisitDate = row.VisitDate,
                    Status = row.Status,
                    StatusText = GetStatusLabel(row.Status),
                    CareLinesText = careLinesByVisit.TryGetValue(row.Id, out var careLines) ? careLines : string.Empty,
                    Notes = row.Notes ?? string.Empty
                }).ToList()
            })
            .ToList();
    }

    public async Task<List<VisitFamilyMemberOptionDto>> GetFamilyVisitOptionsAsync(int houseId, int familyId, DateTime visitDate)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        var patients = await _connection.Table<Patient>()
            .Where(patient => patient.UserId == userId &&
                              patient.IsActive &&
                              patient.HouseId == houseId &&
                              patient.FamilyId == familyId)
            .ToListAsync();

        var options = new List<VisitFamilyMemberOptionDto>();
        foreach (var patient in patients.OrderBy(patient => patient.Name ?? string.Empty, StringComparer.CurrentCultureIgnoreCase))
        {
            var careLines = await GetCareLinesAsync(patient, visitDate);
            options.Add(new VisitFamilyMemberOptionDto
            {
                PatientId = patient.Id,
                PatientName = patient.Name,
                CareLinesText = string.Join(", ", careLines.Select(type => GetCareLineLabel(type.ToString())).Where(label => !string.IsNullOrWhiteSpace(label))),
                Status = VisitStatus.NoVisit
            });
        }

        return options;
    }

    public async Task<List<VisitSuggestionDto>> GetVisitSuggestionsAsync(DateTime referenceDate)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        await PurgeExpiredVisitsAsync(userId, referenceDate);
        var patients = await _connection.Table<Patient>()
            .Where(patient => patient.UserId == userId && patient.IsActive)
            .ToListAsync();

        var suggestions = new List<VisitSuggestionDto>();
        foreach (var patient in patients)
        {
            var careLines = await GetCareLinesAsync(patient, referenceDate);
            if (careLines.Count == 0)
            {
                continue;
            }

            var priority = VisitPriorityCalculator.Calculate(careLines);
            var lastVisit = await GetLastCompletedVisitDateAsync(userId, patient.Id);

            foreach (var careLine in careLines)
            {
                var rule = VisitScoringRuleCatalog.GetRule(careLine);
                if (rule is null)
                {
                    continue;
                }

                var completedVisits = await CountCompletedVisitDaysInReferenceMonthAsync(userId, patient.Id, referenceDate);
                var missingVisits = Math.Max(0, rule.RequiredVisits - completedVisits);
                if (missingVisits <= 0)
                {
                    continue;
                }

                suggestions.Add(new VisitSuggestionDto
                {
                    PatientId = patient.Id,
                    HouseId = patient.HouseId,
                    FamilyId = patient.FamilyId,
                    PatientName = patient.Name,
                    FamilyName = BuildSuggestionFamilyName(patient),
                    CareLineType = careLine.ToString(),
                    PriorityFactor = priority.Factor,
                    PriorityLabel = priority.Label,
                    RequiredVisits = rule.RequiredVisits,
                    CompletedVisits = completedVisits,
                    MissingVisits = missingVisits,
                    HasNoCompletedVisitsInRecommendedPeriod = completedVisits == 0,
                    Points = rule.Points,
                    Reason = BuildSuggestionReason(careLine),
                    LastVisitDate = lastVisit
                });
            }
        }

        return MergePatientSuggestions(suggestions)
            .OrderByDescending(suggestion => suggestion.HasNoCompletedVisitsInRecommendedPeriod)
            .ThenByDescending(suggestion => suggestion.PriorityFactor)
            .ThenByDescending(suggestion => suggestion.Points)
            .ThenByDescending(suggestion => suggestion.MissingVisits)
            .ThenBy(suggestion => suggestion.LastVisitDate ?? DateTime.MinValue)
            .ThenBy(suggestion => suggestion.PatientName)
            .ToList();
    }

    public async Task PurgeExpiredVisitsAsync(DateTime referenceDate)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        await PurgeExpiredVisitsAsync(userId, referenceDate);
    }

    public async Task RegisterVisitAsync(Visits visit)
    {
        ArgumentNullException.ThrowIfNull(visit);

        var userId = currentUserContext.RequireCurrentUserId();
        visit.UserId = userId;
        visit.VisitDate = visit.VisitDate == default ? visit.Date : visit.VisitDate;
        visit.Date = visit.Date == default ? visit.VisitDate : visit.Date;
        visit.Status = NormalizeStatus(visit.Status, visit.Description);
        visit.Description = GetStatusLabel(visit.Status);
        visit.CreatedAt = visit.CreatedAt == default ? DateTime.Now : visit.CreatedAt;

        if (visit.PatientId > 0)
        {
            await EnsureNoDuplicateVisitAsync(userId, visit.PatientId, visit.VisitDate);
        }

        await _connection.InsertAsync(visit);

        if (visit.PatientId > 0)
        {
            var patient = await _connection.Table<Patient>()
                .FirstOrDefaultAsync(item => item.UserId == userId && item.Id == visit.PatientId);
            if (patient is not null)
            {
                await InsertCareLineSnapshotAsync(userId, visit.Id, patient, visit.VisitDate);
            }
        }

        DataChangeTracker.MarkVisitsChanged();
    }

    public async Task<int> RegisterFamilyVisitBatchAsync(VisitBatchRequestDto request)
    {
        if (request.HouseId <= 0 || request.FamilyId <= 0)
        {
            throw new ArgumentException("Familia invalida para registro de visita.");
        }

        var selectedPeople = request.People
            .Where(item => item.Status is VisitStatus.Completed or VisitStatus.Absent or VisitStatus.Refused)
            .GroupBy(item => item.PatientId)
            .Select(group => group.First())
            .ToList();

        if (selectedPeople.Count == 0)
        {
            return 0;
        }

        var userId = currentUserContext.RequireCurrentUserId();
        var visitDate = request.VisitDate == default ? DateTime.Now : request.VisitDate;
        var patientIds = selectedPeople.Select(item => item.PatientId).ToHashSet();
        var patients = (await _connection.Table<Patient>()
                .Where(patient => patient.UserId == userId && patient.HouseId == request.HouseId && patient.FamilyId == request.FamilyId)
                .ToListAsync())
            .Where(patient => patientIds.Contains(patient.Id))
            .ToDictionary(patient => patient.Id);

        if (patients.Count != selectedPeople.Count)
        {
            throw new InvalidOperationException("Uma ou mais pessoas selecionadas nao pertencem a familia.");
        }

        foreach (var person in selectedPeople)
        {
            await EnsureNoDuplicateVisitAsync(userId, person.PatientId, visitDate);
        }

        var batch = new VisitBatch
        {
            UserId = userId,
            HouseId = request.HouseId,
            FamilyId = request.FamilyId,
            VisitDate = visitDate,
            CreatedAt = DateTime.Now,
            Notes = request.Notes?.Trim() ?? string.Empty
        };

        await _connection.InsertAsync(batch);
        var address = await GetAddressAsync(userId, request.HouseId);

        foreach (var item in selectedPeople)
        {
            var patient = patients[item.PatientId];
            var status = item.Status.ToString();
            var visit = new Visits
            {
                UserId = userId,
                PatientId = patient.Id,
                HouseId = request.HouseId,
                FamilyId = request.FamilyId,
                BatchId = batch.Id,
                Date = visitDate,
                VisitDate = visitDate,
                Status = status,
                Description = GetStatusLabel(status),
                Address = address,
                Notes = request.Notes?.Trim() ?? string.Empty,
                CreatedAt = DateTime.Now
            };

            await _connection.InsertAsync(visit);
            await InsertCareLineSnapshotAsync(userId, visit.Id, patient, visit.VisitDate);
        }

        DataChangeTracker.MarkVisitsChanged();
        return selectedPeople.Count;
    }

    public async Task DeleteVisitAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("O ID da visita deve ser maior que zero.");
        }

        var userId = currentUserContext.RequireCurrentUserId();
        await _connection.ExecuteAsync("DELETE FROM VisitCareLine WHERE VisitId = ? AND UserId = ?", id, userId);
        await _connection.ExecuteAsync("DELETE FROM Visits WHERE Id = ? AND UserId = ?", id, userId);
        DataChangeTracker.MarkVisitsChanged();
    }

    private async Task PurgeExpiredVisitsAsync(int userId, DateTime referenceDate)
    {
        var cutoff = referenceDate.Date.AddYears(-1);
        var deletedCareLines = await _connection.ExecuteAsync(
            """
            DELETE FROM VisitCareLine
            WHERE UserId = ?
              AND VisitId IN (
                  SELECT Id
                  FROM Visits
                  WHERE UserId = ?
                    AND VisitDate < ?
              )
            """,
            userId,
            userId,
            cutoff);

        var deletedVisits = await _connection.ExecuteAsync(
            "DELETE FROM Visits WHERE UserId = ? AND VisitDate < ?",
            userId,
            cutoff);

        var deletedBatches = await _connection.ExecuteAsync(
            "DELETE FROM VisitBatch WHERE UserId = ? AND VisitDate < ?",
            userId,
            cutoff);

        await _connection.ExecuteAsync(
            """
            DELETE FROM VisitCareLine
            WHERE UserId = ?
              AND VisitId NOT IN (
                  SELECT Id
                  FROM Visits
                  WHERE UserId = ?
              )
            """,
            userId,
            userId);

        if (deletedCareLines > 0 || deletedVisits > 0 || deletedBatches > 0)
        {
            DataChangeTracker.MarkVisitsChanged();
        }
    }

    private static IEnumerable<VisitSuggestionDto> MergePatientSuggestions(IEnumerable<VisitSuggestionDto> suggestions)
    {
        return suggestions
            .GroupBy(suggestion => suggestion.PatientId)
            .Select(group =>
            {
                var orderedItems = group.OrderByDescending(item => item.Points).ToList();
                var first = orderedItems[0];
                var careLines = string.Join(", ", orderedItems.Select(item => GetSuggestionReasonLabel(item.CareLineType)).Where(label => !string.IsNullOrWhiteSpace(label)).Distinct());
                var dueDates = orderedItems.Select(item => item.DueDate).Where(date => date.HasValue).ToList();
                var lastVisitDates = orderedItems.Select(item => item.LastVisitDate).Where(date => date.HasValue).ToList();
                var requiredVisits = orderedItems.Max(item => item.RequiredVisits);
                var completedVisits = orderedItems.Max(item => item.CompletedVisits);
                var missingVisits = Math.Max(0, requiredVisits - completedVisits);

                return new VisitSuggestionDto
                {
                    PatientId = first.PatientId,
                    HouseId = first.HouseId,
                    FamilyId = first.FamilyId,
                    PatientName = first.PatientName,
                    FamilyName = first.FamilyName,
                    CareLineType = careLines,
                    PriorityFactor = first.PriorityFactor,
                    PriorityLabel = first.PriorityLabel,
                    RequiredVisits = requiredVisits,
                    CompletedVisits = completedVisits,
                    MissingVisits = missingVisits,
                    HasNoCompletedVisitsInRecommendedPeriod = completedVisits == 0,
                    Points = orderedItems.Sum(item => item.Points),
                    Reason = careLines,
                    DueDate = dueDates.Count > 0 ? dueDates.Min() : null,
                    IsOverdue = orderedItems.Any(item => item.IsOverdue),
                    LastVisitDate = lastVisitDates.Count > 0 ? lastVisitDates.Max() : null
                };
            });
    }

    private Task<int> CountVisitsAsync(int userId, DateTime start, DateTime end, VisitStatus? status)
    {
        if (status is null)
        {
            return _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Visits WHERE UserId = ? AND PatientId > 0 AND VisitDate >= ? AND VisitDate < ?",
                userId,
                start,
                end);
        }

        return _connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Visits WHERE UserId = ? AND PatientId > 0 AND VisitDate >= ? AND VisitDate < ? AND Status = ?",
            userId,
            start,
            end,
            status.Value.ToString());
    }

    private Task<int> CountCareLineVisitsAsync(int userId, DateTime start, DateTime end, VisitCareLineType careLine)
    {
        return _connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(DISTINCT v.Id)
            FROM Visits v
            INNER JOIN VisitCareLine vc ON vc.UserId = v.UserId AND vc.VisitId = v.Id
            WHERE v.UserId = ?
              AND v.PatientId > 0
              AND v.VisitDate >= ?
              AND v.VisitDate < ?
              AND vc.CareLineType = ?
            """,
            userId,
            start,
            end,
            careLine.ToString());
    }

    private async Task EnsureNoDuplicateVisitAsync(int userId, int patientId, DateTime visitDate)
    {
        var start = visitDate.Date;
        var end = start.AddDays(1);
        var exists = await _connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Visits WHERE UserId = ? AND PatientId = ? AND VisitDate >= ? AND VisitDate < ?",
            userId,
            patientId,
            start,
            end);

        if (exists > 0)
        {
            throw new InvalidOperationException("Ja existe visita registrada para esta pessoa nesta data.");
        }
    }

    private async Task InsertCareLineSnapshotAsync(int userId, int visitId, Patient patient, DateTime referenceDate)
    {
        var careLines = await GetCareLinesAsync(patient, referenceDate);
        foreach (var careLine in careLines.Distinct())
        {
            await _connection.InsertAsync(new VisitCareLine
            {
                UserId = userId,
                VisitId = visitId,
                CareLineType = careLine.ToString()
            });
        }
    }

    private async Task<List<VisitCareLineType>> GetCareLinesAsync(Patient patient, DateTime referenceDate)
    {
        var careLines = new List<VisitCareLineType>();

        if (patient.BirthDate > referenceDate.Date.AddYears(-2))
        {
            careLines.Add(VisitCareLineType.Child);
        }

        if (patient.BirthDate <= referenceDate.Date.AddYears(-60))
        {
            careLines.Add(VisitCareLineType.Elderly);
        }

        var conditions = await _connection.Table<PatientConditions>()
            .Where(condition => condition.UserId == patient.UserId && condition.PatientId == patient.Id)
            .ToListAsync();

        foreach (var condition in conditions)
        {
            var key = HealthConditionCatalog.GetKey(condition.Description ?? string.Empty);
            var normalized = SearchTextNormalizer.Normalize(key);

            if (string.Equals(key, HealthConditionCatalog.Gestante, StringComparison.OrdinalIgnoreCase))
            {
                careLines.Add(VisitCareLineType.Pregnancy);
            }
            else if (normalized.Contains("puerper", StringComparison.OrdinalIgnoreCase))
            {
                careLines.Add(VisitCareLineType.Postpartum);
            }
            else if (string.Equals(key, HealthConditionCatalog.Diabetes, StringComparison.OrdinalIgnoreCase))
            {
                careLines.Add(VisitCareLineType.Diabetes);
            }
            else if (normalized.Contains("hipertens", StringComparison.OrdinalIgnoreCase))
            {
                careLines.Add(VisitCareLineType.Hypertension);
            }
        }

        var hasBolsaFamilia = await _connection.Table<PatientBolsaFamilia>()
            .Where(item => item.UserId == patient.UserId && item.PatientId == patient.Id)
            .CountAsync() > 0;

        if (hasBolsaFamilia)
        {
            careLines.Add(VisitCareLineType.BolsaFamilia);
        }

        if (careLines.Count == 0)
        {
            careLines.Add(VisitCareLineType.NoVulnerability);
        }

        return careLines.Distinct().ToList();
    }

    private async Task<int> CountCompletedVisitDaysInReferenceMonthAsync(
        int userId,
        int patientId,
        DateTime referenceDate)
    {
        var start = new DateTime(referenceDate.Year, referenceDate.Month, 1);
        var end = start.AddMonths(1);

        var visits = await _connection.QueryAsync<VisitDateRow>(
            """
            SELECT DISTINCT v.VisitDate
            FROM Visits v
            WHERE v.UserId = ?
              AND v.PatientId = ?
              AND v.Status = ?
              AND v.VisitDate >= ?
              AND v.VisitDate < ?
            ORDER BY v.VisitDate ASC
            """,
            userId,
            patientId,
            VisitStatus.Completed.ToString(),
            start,
            end);

        return visits
            .Select(visit => visit.VisitDate.Date)
            .Distinct()
            .Count();
    }

    private Task<DateTime?> GetLastCompletedVisitDateAsync(int userId, int patientId)
    {
        return _connection.ExecuteScalarAsync<DateTime?>(
            """
            SELECT MAX(VisitDate)
            FROM Visits
            WHERE UserId = ?
              AND PatientId = ?
              AND Status = ?
            """,
            userId,
            patientId,
            VisitStatus.Completed.ToString());
    }

    private async Task<string> GetAddressAsync(int userId, int houseId)
    {
        var house = await _connection.Table<House>().FirstOrDefaultAsync(item => item.UserId == userId && item.CasaId == houseId);
        if (house is null)
        {
            return string.Empty;
        }

        var number = string.IsNullOrWhiteSpace(house.NumeroCasa) ? "S/N" : house.NumeroCasa.Trim();
        return house.PossuiComplemento && !string.IsNullOrWhiteSpace(house.Complemento)
            ? $"{house.Rua}, {number} - {house.Complemento.Trim()}"
            : $"{house.Rua}, {number}";
    }

    private static string NormalizeStatus(string? status, string? description)
    {
        if (Enum.TryParse<VisitStatus>(status, ignoreCase: true, out var parsed) && parsed != VisitStatus.NoVisit)
        {
            return parsed.ToString();
        }

        return description?.Trim() switch
        {
            "Realizada" => VisitStatus.Completed.ToString(),
            "Ausente" => VisitStatus.Absent.ToString(),
            "Recusada" => VisitStatus.Refused.ToString(),
            _ => VisitStatus.Completed.ToString()
        };
    }

    private static string GetStatusLabel(string? status)
    {
        return status switch
        {
            nameof(VisitStatus.Completed) => "Realizada",
            nameof(VisitStatus.Absent) => "Ausente",
            nameof(VisitStatus.Refused) => "Recusada",
            _ => status ?? string.Empty
        };
    }

    private static string GetCareLineLabel(string? careLine)
    {
        return careLine switch
        {
            nameof(VisitCareLineType.Child) => "Criança",
            nameof(VisitCareLineType.Pregnancy) => "Gestante",
            nameof(VisitCareLineType.Postpartum) => "Puérpera",
            nameof(VisitCareLineType.Hypertension) => "Hipertenso",
            nameof(VisitCareLineType.Diabetes) => "Diabético",
            nameof(VisitCareLineType.Elderly) => "Idoso",
            nameof(VisitCareLineType.BolsaFamilia) => "Bolsa Família",
            nameof(VisitCareLineType.Bpc) => "BPC",
            nameof(VisitCareLineType.NoVulnerability) => "Sem critérios de vulnerabilidade",
            _ => string.Empty
        };
    }

    private static string BuildSuggestionFamilyName(Patient patient)
    {
        if (patient.HouseId <= 0 && patient.FamilyId <= 0)
        {
            return "Sem residência/família vinculada";
        }

        if (patient.HouseId <= 0)
        {
            return $"Familia {patient.FamilyId} sem residência vinculada";
        }

        if (patient.FamilyId <= 0)
        {
            return "Sem família vinculada";
        }

        return $"Família {patient.FamilyId}";
    }

    private static string BuildSuggestionReason(VisitCareLineType careLine)
    {
        return GetSuggestionReasonLabel(careLine.ToString());
    }

    private static string GetSuggestionReasonLabel(string? careLine)
    {
        return careLine switch
        {
            nameof(VisitCareLineType.Child) => "Criança até 2 anos",
            nameof(VisitCareLineType.NoVulnerability) => "Sem critérios de vulnerabilidade",
            _ => GetCareLineLabel(careLine)
        };
    }

    private sealed class VisitRecordRow
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public int HouseId { get; set; }
        public int FamilyId { get; set; }
        public DateTime VisitDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string FamilyName { get; set; } = string.Empty;
    }

    private sealed class VisitCareLineRow
    {
        public int VisitId { get; set; }
        public string CareLineType { get; set; } = string.Empty;
    }

    private sealed class VisitDateRow
    {
        public DateTime VisitDate { get; set; }
    }
}
