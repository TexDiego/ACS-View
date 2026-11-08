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
                CASE WHEN v.FamilyId <= 0 THEN 'Sem família vinculada'
                     ELSE COALESCE(NULLIF(TRIM(responsible.Name), ''), 'Família ' || v.FamilyId)
                END AS FamilyName
            FROM Visits v
            LEFT JOIN Patient p ON p.UserId = v.UserId AND p.Id = v.PatientId
            LEFT JOIN LegacyFamilyMap fm ON fm.UserId=v.UserId AND fm.LegacyHouseId=v.HouseId
                AND fm.LocalNumber=v.FamilyId AND fm.SourcePatientId=0
            LEFT JOIN FamilyRecord family ON family.Id=fm.FamilyId AND family.UserId=v.UserId
            LEFT JOIN Patient responsible
                ON responsible.UserId = v.UserId
               AND responsible.Id = family.ResponsiblePatientId
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

    public async Task<PatientVisitSummaryDto> GetPatientVisitSummaryAsync(int patientId, DateTime referenceDate)
    {
        var data = await LoadFollowUpDataAsync(patientId);
        var patient = data.Patients.SingleOrDefault();
        return patient is null ? new PatientVisitSummaryDto { PatientId = patientId } : await Task.Run(() => BuildSummary(patient, data, referenceDate));
    }

    public async Task<List<VisitSuggestionDto>> GetVisitSuggestionsAsync(DateTime referenceDate)
    {
        var data = await LoadFollowUpDataAsync(null);
        return await Task.Run(() => data.Patients.Select(patient =>
        {
            var summary = BuildSummary(patient, data, referenceDate);
            var primary = summary.Primary;
            if (primary is null || summary.Evaluations.All(e => e.Urgency == 0)) return null;
            var priority = VisitPriorityCalculator.Calculate(summary.Evaluations.Select(e => e.CareLine));
            return new VisitSuggestionDto
            {
                PatientId = patient.Id, HouseId = patient.HouseId, FamilyId = patient.FamilyId,
                PatientName = patient.Name, FamilyName = BuildSuggestionFamilyName(patient),
                CareLineType = string.Join(", ", summary.Evaluations.Select(e => GetCareLineLabel(e.CareLine.ToString()))),
                PriorityFactor = priority.Factor, PriorityLabel = priority.Label,
                RequiredVisits = primary.Required, CompletedVisits = primary.Completed, MissingVisits = primary.Missing,
                HasNoCompletedVisitsInRecommendedPeriod = primary.Completed == 0,
                Points = summary.Evaluations.Sum(e => VisitScoringRuleCatalog.GetRule(e.CareLine)!.Points),
                Reason = string.Join(", ", summary.Evaluations.Select(e => GetCareLineLabel(e.CareLine.ToString()))),
                DueDate = primary.Deadline, IsOverdue = primary.Status == VisitFollowUpStatus.Overdue,
                LastVisitDate = summary.LastCompletedVisit, Summary = summary,
                FollowUpText = $"{primary.StatusText}. {primary.Progress}" +
                    (primary.RecommendedDate.HasValue ? $" Planejamento: {primary.RecommendedDate:dd/MM/yyyy}." : "") +
                    (primary.Deadline.HasValue ? $" Prazo da regra: {primary.Deadline:dd/MM/yyyy}." : "")
            };
        }).Where(s => s != null).Select(s => s!).OrderByDescending(s => s.Summary!.Primary!.Urgency)
          .ThenBy(s => s.DueDate ?? DateTime.MaxValue).ThenByDescending(s => s.PriorityFactor)
          .ThenByDescending(s => s.Points).ThenBy(s => s.PatientName).ToList());
    }

    // Fixed number of scoped queries; the popup uses the same pipeline with a patient predicate.
    private async Task<FollowUpData> LoadFollowUpDataAsync(int? patientId)
    {
        var user = currentUserContext.RequireCurrentUserId();
        var args = patientId.HasValue ? new object[] { user, patientId.Value } : new object[] { user };
        var predicate = patientId.HasValue ? " AND PatientId=?" : "";
        var birthSql = ACS_View.Infrastructure.Data.Migrations.LegacyDateSql.AsTicks("\"BirthDate\"");
        var patientProjection = ACS_View.Infrastructure.Data.Migrations.LegacyDateSql.Projection<Patient>()
            .Replace(birthSql, $"COALESCE({birthSql},0)");
        var patients = await _connection.QueryAsync<Patient>(
            $"SELECT {patientProjection} FROM Patient WHERE UserId=?" +
            (patientId.HasValue ? " AND Id=?" : " AND COALESCE(IsActive,1)=1"), args);
        var conditions = await _connection.QueryAsync<PatientConditions>("SELECT * FROM PatientConditions WHERE UserId=?" + predicate,args);
        var benefits = await _connection.QueryAsync<PatientBolsaFamilia>("SELECT * FROM PatientBolsaFamilia WHERE UserId=?" + predicate,args);
        var pregnancies = await _connection.QueryAsync<PatientPregnancy>($"SELECT {ACS_View.Infrastructure.Data.Migrations.LegacyDateSql.Projection<PatientPregnancy>()} FROM PatientPregnancy WHERE UserId=?" + predicate,args);
        var visits = await _connection.QueryAsync<Visits>($"SELECT {ACS_View.Infrastructure.Data.Migrations.LegacyDateSql.Projection<Visits>()} FROM Visits WHERE UserId=?" + predicate,args);
        var lines = await _connection.QueryAsync<VisitCareLine>("SELECT vc.* FROM VisitCareLine vc INNER JOIN Visits v ON v.Id=vc.VisitId AND v.UserId=vc.UserId WHERE vc.UserId=?" +
            (patientId.HasValue ? " AND v.PatientId=?" : ""),args);
        return new(patients,conditions.ToLookup(c=>c.PatientId ?? 0),benefits.Select(b=>b.PatientId).ToHashSet(),
            pregnancies.ToLookup(p=>p.PatientId),visits.ToLookup(v=>v.PatientId),lines.ToLookup(l=>l.VisitId));
    }

    private static PatientVisitSummaryDto BuildSummary(Patient patient, FollowUpData data, DateTime referenceDate)
    {
        var today = referenceDate.Date;
        var lines = new HashSet<VisitCareLineType>();
        DateTime? birth = patient.BirthDate > DateTime.MinValue && patient.BirthDate <= today ? patient.BirthDate : null;
        if (birth > today.AddYears(-2)) lines.Add(VisitCareLineType.Child);
        if (birth <= today.AddYears(-60)) lines.Add(VisitCareLineType.Elderly);
        var episodes = data.Pregnancies[patient.Id].Where(p => p.CreatedAt.Date <= today).ToList();
        var ambiguousEpisode = episodes.Count(p => p.Status == PregnancyStatus.Active) > 1;
        var episode = episodes
            .OrderByDescending(p => PregnancyCalculator.IsPuerperal(p,today)).ThenByDescending(p=>p.Status==PregnancyStatus.Active)
            .ThenByDescending(p=>p.CreatedAt).ThenByDescending(p=>p.Id).FirstOrDefault();
        if (ambiguousEpisode) episode = null;
        if (episode != null && PregnancyCalculator.IsPuerperal(episode,today)) lines.Add(VisitCareLineType.Postpartum);
        else if (episode?.Status == PregnancyStatus.Active || ambiguousEpisode) lines.Add(VisitCareLineType.Pregnancy);
        foreach (var condition in data.Conditions[patient.Id])
        {
            var key = HealthConditionCatalog.GetKey(condition.Description ?? "");
            var normalized = SearchTextNormalizer.Normalize(key);
            if (key == HealthConditionCatalog.Diabetes) lines.Add(VisitCareLineType.Diabetes);
            else if (normalized.Contains("hipertens")) lines.Add(VisitCareLineType.Hypertension);
            else if (key == HealthConditionCatalog.Gestante && episode == null) lines.Add(VisitCareLineType.Pregnancy);
            else if (normalized.Contains("puerper") && episode == null) lines.Add(VisitCareLineType.Postpartum);
        }
        if (data.Benefits.Contains(patient.Id)) lines.Add(VisitCareLineType.BolsaFamilia);
        if (lines.Count == 0) lines.Add(VisitCareLineType.NoVulnerability);
        var history = data.Visits[patient.Id].Where(v=>v.VisitDate.Date<=today).ToList();
        var completed = history.Where(v=>v.Status==nameof(VisitStatus.Completed)).ToList();
        var evaluations = lines.Select(line => VisitFollowUpCalculator.Evaluate(line,birth,episode,
            completed.Where(v=>line == VisitCareLineType.NoVulnerability || data.Lines[v.Id].Any(l=>l.CareLineType==line.ToString())).Select(v=>v.VisitDate),
            completed.Any(v=>!data.Lines[v.Id].Any()),today)).ToList();
        var last = history.OrderByDescending(v=>v.VisitDate).FirstOrDefault();
        return new PatientVisitSummaryDto { PatientId=patient.Id, ReferenceMonth=today,
            MonthlyCompletedVisits=completed.Count(v=>v.VisitDate.Year==today.Year && v.VisitDate.Month==today.Month),
            LastCompletedVisit=completed.Select(v=>(DateTime?)v.VisitDate).Max(), LastAttempt=last?.VisitDate,
            LastAttemptStatus=last?.Status ?? "", Evaluations=evaluations,
            DataQualityNote = ambiguousEpisode ? "Há mais de um episódio gestacional ativo; revisar o cadastro." :
                birth is null ? "Nascimento ausente ou inconsistente: elegibilidade por idade não avaliada." : "" };
    }

    private sealed record FollowUpData(List<Patient> Patients, ILookup<int,PatientConditions> Conditions,
        HashSet<int> Benefits, ILookup<int,PatientPregnancy> Pregnancies, ILookup<int,Visits> Visits,
        ILookup<int,VisitCareLine> Lines);

    public async Task PurgeExpiredVisitsAsync(DateTime referenceDate)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        await PurgeExpiredVisitsAsync(userId, referenceDate);
    }

    private readonly SemaphoreSlim quickVisitGate = new(1, 1);

    public async Task<bool> RegisterQuickVisitAsync(int patientId, Func<string, string, string, Task<bool>> confirm)
    {
        if (!await quickVisitGate.WaitAsync(0)) return false;
        try
        {
            var userId = currentUserContext.RequireCurrentUserId();
            var session = DataChangeTracker.SessionVersion;
            var patient = await _connection.Table<Patient>().FirstOrDefaultAsync(p => p.Id == patientId && p.UserId == userId)
                ?? throw new InvalidOperationException("Pessoa indisponível neste perfil.");
            if (!await confirm("Registrar visita rápida",
                $"Deseja registrar uma visita realizada agora para esta pessoa?\n\n{patient.Name}\n{DateTime.Now:dd/MM/yyyy HH:mm}", "Confirmar visita")) return false;
            var now = DateTime.Now;
            var existingCount = await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Visits WHERE UserId=? AND PatientId=? AND Status=? AND VisitDate>=? AND VisitDate<?",
                userId, patientId, nameof(VisitStatus.Completed), now.Date, now.Date.AddDays(1));
            if (existingCount > 0 && !await confirm("Visita já registrada",
                "Esta pessoa já possui uma visita registrada hoje. Deseja registrar outra visita mesmo assim?", "Registrar outra visita")) return false;
            if (currentUserContext.RequireCurrentUserId() != userId || session != DataChangeTracker.SessionVersion)
                throw new InvalidOperationException("O perfil foi alterado. Abra novamente o cadastro.");
            var visit = new Visits { PatientId = patientId, HouseId = patient.HouseId, FamilyId = patient.FamilyId,
                VisitDate = now, Date = now, Status = nameof(VisitStatus.Completed), Address = await GetAddressAsync(userId, patient.HouseId) };
            await RegisterVisitCoreAsync(visit, true, userId, session, existingCount);
            return true;
        }
        finally { quickVisitGate.Release(); }
    }

    public Task RegisterVisitAsync(Visits visit) => RegisterVisitCoreAsync(visit);

    private async Task RegisterVisitCoreAsync(Visits visit, bool quick = false, int? expectedUser = null, int? expectedSession = null, int expectedCount = 0)
    {
        ArgumentNullException.ThrowIfNull(visit);

        var userId = currentUserContext.RequireCurrentUserId();
        visit.UserId = userId;
        visit.VisitDate = visit.VisitDate == default ? visit.Date : visit.VisitDate;
        visit.Date = visit.Date == default ? visit.VisitDate : visit.Date;
        visit.Status = NormalizeStatus(visit.Status, visit.Description);
        visit.Description = GetStatusLabel(visit.Status);
        visit.CreatedAt = visit.CreatedAt == default ? DateTime.Now : visit.CreatedAt;

        if (!quick && visit.PatientId > 0)
        {
            await EnsureNoDuplicateVisitAsync(userId, visit.PatientId, visit.VisitDate);
        }

        var careLines=new List<VisitCareLineType>();
        if (visit.PatientId > 0)
        {
            var patient = await _connection.Table<Patient>()
                .FirstOrDefaultAsync(item => item.UserId == userId && item.Id == visit.PatientId);
            if (patient is not null)
            {
                careLines=await GetCareLinesAsync(patient,visit.VisitDate);
            }
        }
        await _connection.RunInTransactionAsync(c=>
        {
            if (quick)
            {
                if (c.ExecuteScalar<int>("SELECT COUNT(*) FROM Visits WHERE UserId=? AND PatientId=? AND Status=? AND VisitDate>=? AND VisitDate<?",
                    userId, visit.PatientId, nameof(VisitStatus.Completed), visit.VisitDate.Date, visit.VisitDate.Date.AddDays(1)) != expectedCount)
                    throw new InvalidOperationException("O histórico foi alterado. Tente novamente para confirmar a nova visita.");
                if (currentUserContext.RequireCurrentUserId() != expectedUser || DataChangeTracker.SessionVersion != expectedSession ||
                    c.Table<Patient>().FirstOrDefault(p => p.Id == visit.PatientId && p.UserId == userId) is null)
                    throw new InvalidOperationException("Pessoa ou perfil alterado; tente novamente.");
            }
            else EnsureNoDuplicateVisit(c,userId,visit.PatientId,visit.VisitDate);
            c.Insert(visit);
            foreach(var line in careLines.Distinct())
                c.Insert(new VisitCareLine{UserId=userId,VisitId=visit.Id,CareLineType=line.ToString()});
        });

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

        var address = await GetAddressAsync(userId, request.HouseId);
        var snapshots=new Dictionary<int,List<VisitCareLineType>>();
        foreach(var patient in patients.Values)
            snapshots[patient.Id]=await GetCareLinesAsync(patient,visitDate);
        await _connection.RunInTransactionAsync(c=>
        {
          foreach(var patient in patients.Values)
          {
            var current=c.Table<Patient>().FirstOrDefault(p=>p.Id==patient.Id && p.UserId==userId && p.HouseId==request.HouseId && p.FamilyId==request.FamilyId);
            if(current==null)throw new InvalidOperationException("Vínculo familiar alterado; recarregue os membros.");
            EnsureNoDuplicateVisit(c,userId,patient.Id,visitDate);
          }
          c.Insert(batch);
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

            c.Insert(visit);
            foreach(var line in snapshots[patient.Id].Distinct())
                c.Insert(new VisitCareLine{UserId=userId,VisitId=visit.Id,CareLineType=line.ToString()});
          }
        });

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
        await _connection.RunInTransactionAsync(c=>
        {
            c.Execute("DELETE FROM VisitCareLine WHERE VisitId = ? AND UserId = ?",id,userId);
            c.Execute("DELETE FROM Visits WHERE Id = ? AND UserId = ?",id,userId);
        });
        DataChangeTracker.MarkVisitsChanged();
    }

    // Kept for caller compatibility; historical events are retained until explicit deletion.
    private Task PurgeExpiredVisitsAsync(int userId, DateTime referenceDate) => Task.CompletedTask;

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

    private static void EnsureNoDuplicateVisit(SQLiteConnection c,int userId,int patientId,DateTime visitDate)
    {
        if(patientId>0 && c.ExecuteScalar<int>("SELECT COUNT(*) FROM Visits WHERE UserId=? AND PatientId=? AND VisitDate>=? AND VisitDate<?",userId,patientId,visitDate.Date,visitDate.Date.AddDays(1))>0)
            throw new InvalidOperationException("Já existe visita para esta pessoa nesta data.");
    }

    private async Task<List<VisitCareLineType>> GetCareLinesAsync(Patient patient, DateTime referenceDate)
    {
        var careLines = new List<VisitCareLineType>();

        var hasBirthDate=await _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Patient WHERE Id=? AND UserId=? AND BirthDate IS NOT NULL AND BirthDate<>0",patient.Id,patient.UserId)>0;
        if (hasBirthDate && patient.BirthDate <= referenceDate.Date && patient.BirthDate > referenceDate.Date.AddYears(-2))
        {
            careLines.Add(VisitCareLineType.Child);
        }

        if (hasBirthDate && patient.BirthDate <= referenceDate.Date.AddYears(-60))
        {
            careLines.Add(VisitCareLineType.Elderly);
        }

        var conditions = await _connection.Table<PatientConditions>()
            .Where(condition => condition.UserId == patient.UserId && condition.PatientId == patient.Id)
            .ToListAsync();

        var pregnancyCareLine = await GetPregnancyCareLineAsync(patient, referenceDate);
        if (pregnancyCareLine is not null)
        {
            careLines.Add(pregnancyCareLine.Value);
        }

        foreach (var condition in conditions)
        {
            var key = HealthConditionCatalog.GetKey(condition.Description ?? string.Empty);
            var normalized = SearchTextNormalizer.Normalize(key);

            if (string.Equals(key, HealthConditionCatalog.Gestante, StringComparison.OrdinalIgnoreCase))
            {
                if (pregnancyCareLine is null)
                {
                    careLines.Add(VisitCareLineType.Pregnancy);
                }
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

    private async Task<VisitCareLineType?> GetPregnancyCareLineAsync(Patient patient, DateTime referenceDate)
    {
        var pregnancies = await _connection.QueryAsync<PatientPregnancy>($"SELECT {ACS_View.Infrastructure.Data.Migrations.LegacyDateSql.Projection<PatientPregnancy>()} FROM PatientPregnancy WHERE UserId=? AND PatientId=?",patient.UserId,patient.Id);

        var pregnancy = pregnancies
            .OrderByDescending(item => PregnancyCalculator.IsPuerperal(item, referenceDate))
            .ThenByDescending(item => item.Status == PregnancyStatus.Active)
            .ThenByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .FirstOrDefault();

        if (pregnancy is null)
        {
            return null;
        }

        if (PregnancyCalculator.IsPuerperal(pregnancy, referenceDate))
        {
            return VisitCareLineType.Postpartum;
        }

        return pregnancy.Status == PregnancyStatus.Active
            ? VisitCareLineType.Pregnancy
            : null;
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

}
