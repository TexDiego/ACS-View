using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using SQLite;
using System.Collections.ObjectModel;

namespace ACS_View.Infrastructure.Data.SQLite;

internal sealed class SQLitePatientBolsaFamiliaRepository(
    IDatabaseService databaseService,
    ICurrentUserContext currentUserContext) : IPatientBolsaFamiliaRepository
{
    private readonly SQLiteAsyncConnection _connection = databaseService.Connection;

    public Task<PatientBolsaFamilia?> GetByPatientIdAsync(int patientId)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        return _connection.Table<PatientBolsaFamilia>()
            .FirstOrDefaultAsync(item => item.UserId == userId && item.PatientId == patientId);
    }

    public async Task<List<BolsaFamiliaGroup>> GetGroupsAsync()
    {
        var userId = currentUserContext.RequireCurrentUserId();
        var rows = await _connection.QueryAsync<BolsaFamiliaGroupRow>(
            """
            SELECT
                bf.ResponsiblePatientId,
                COALESCE(NULLIF(TRIM(responsible.Name), ''), 'Responsavel nao encontrado') AS ResponsibleName,
                patient.Id AS PatientId,
                patient.Name AS PatientName,
                patient.SusNumber,
                bf.NisNumber
            FROM PatientBolsaFamilia bf
            INNER JOIN Patient patient
                ON patient.UserId = bf.UserId
               AND patient.Id = bf.PatientId
            LEFT JOIN Patient responsible
                ON responsible.UserId = bf.UserId
               AND responsible.Id = bf.ResponsiblePatientId
            WHERE bf.UserId = ?
              AND COALESCE(patient.IsActive, 1) = 1
            ORDER BY ResponsibleName COLLATE NOCASE, bf.ResponsiblePatientId, patient.Name COLLATE NOCASE, patient.Id
            """,
            userId);

        return rows
            .GroupBy(row => new { row.ResponsiblePatientId, row.ResponsibleName })
            .Select(group =>
            {
                var beneficiaries = group
                    .OrderBy(row => row.PatientName ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(row => row.PatientId)
                    .Select(row => new BolsaFamiliaBeneficiary
                    {
                        PatientId = row.PatientId,
                        PatientName = row.PatientName ?? string.Empty,
                        SusNumber = row.SusNumber ?? string.Empty,
                        NisNumber = row.NisNumber ?? string.Empty
                    })
                    .ToList();

                return new BolsaFamiliaGroup
                {
                    ResponsiblePatientId = group.Key.ResponsiblePatientId,
                    ResponsibleName = group.Key.ResponsibleName ?? string.Empty,
                    BeneficiaryCount = beneficiaries.Count,
                    Beneficiaries = new ObservableCollection<BolsaFamiliaBeneficiary>(beneficiaries)
                };
            })
            .ToList();
    }

    public async Task UpsertAsync(PatientBolsaFamilia benefit)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        benefit.UserId = userId;
        benefit.NisNumber = benefit.NisNumber?.Trim() ?? string.Empty;

        var existing = await GetByPatientIdAsync(benefit.PatientId);
        if (existing is null)
        {
            await _connection.InsertAsync(benefit);
            DataChangeTracker.MarkPatientsChanged();
            return;
        }

        existing.ResponsiblePatientId = benefit.ResponsiblePatientId;
        existing.NisNumber = benefit.NisNumber;
        await _connection.UpdateAsync(existing);
        DataChangeTracker.MarkPatientsChanged();
    }

    public async Task DeleteByPatientIdAsync(int patientId)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        await _connection.Table<PatientBolsaFamilia>()
            .Where(item => item.UserId == userId && item.PatientId == patientId)
            .DeleteAsync();
        DataChangeTracker.MarkPatientsChanged();
    }

    private sealed class BolsaFamiliaGroupRow
    {
        public int ResponsiblePatientId { get; set; }
        public string ResponsibleName { get; set; } = string.Empty;
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string SusNumber { get; set; } = string.Empty;
        public string NisNumber { get; set; } = string.Empty;
    }
}
