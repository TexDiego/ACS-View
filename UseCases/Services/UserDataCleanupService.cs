using ACS_View.Application.Interfaces;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;
using SQLite;

namespace ACS_View.UseCases.Services;

internal sealed class UserDataCleanupService(
    IDatabaseService databaseService,
    ICurrentUserContext currentUserContext,
    INoteReminderService reminders) : IUserDataCleanupService
{
    private readonly SQLiteAsyncConnection _connection = databaseService.Connection;

    public async Task DeleteAsync(UserDataDeletionScope scope)
    {
        var userId = currentUserContext.RequireCurrentUserId();

        await _connection.RunInTransactionAsync(connection =>
        {
            switch (scope)
            {
                case UserDataDeletionScope.All:
                    DeleteAll(connection, userId);
                    break;
                case UserDataDeletionScope.Patients:
                    DeletePatients(connection, userId);
                    break;
                case UserDataDeletionScope.Houses:
                    DeleteHouses(connection, userId);
                    break;
                case UserDataDeletionScope.Notes:
                    DeleteNotes(connection, userId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope), scope, "Opcao de limpeza invalida.");
            }
        });

        MarkChanged(scope);
        if (scope is UserDataDeletionScope.All or UserDataDeletionScope.Notes)
            await reminders.ReconcileAsync();
    }

    private static void DeleteAll(SQLiteConnection connection, int userId)
    {
        DeletePatients(connection, userId);
        connection.Execute("DELETE FROM TerritorialAssignment WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM House WHERE UserId = ?", userId);
        DeleteNotes(connection, userId);
    }

    private static void DeletePatients(SQLiteConnection connection, int userId)
    {
        connection.Execute("DELETE FROM ImportHistory WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM MigrationIssue WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM PatientPhone WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM PatientSusNumber WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM FamilyMember WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM FamilyResidenceHistory WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM LegacyFamilyMap WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM FamilyRecord WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM PatientPregnancy WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM CareNotification WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM PatientCID WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM PatientConditions WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM PatientBolsaFamilia WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM PatientInsulinDependency WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM PatientVaccineDose WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM Vaccines WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM VisitCareLine WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM VisitBatch WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM Visits WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM Family WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM Patient WHERE UserId = ?", userId);
    }

    private static void DeleteHouses(SQLiteConnection connection, int userId)
    {
        connection.Execute("UPDATE FamilyResidenceHistory SET EndedAt=? WHERE UserId=? AND EndedAt IS NULL",DateTime.UtcNow,userId);
        connection.Execute("UPDATE FamilyRecord SET HouseId=NULL, ResponsiblePatientId=NULL WHERE UserId=?",userId);
        connection.Execute("UPDATE FamilyMember SET LeftAt=?, UpdatedAt=? WHERE UserId=? AND LeftAt IS NULL",DateTime.UtcNow,DateTime.UtcNow,userId);
        connection.Execute("UPDATE TerritorialAssignment SET ValidTo=? WHERE UserId=? AND ValidTo IS NULL",DateTime.UtcNow,userId);
        connection.Execute("UPDATE House SET IsActive=0 WHERE UserId=?",userId);
        connection.Execute(
            """
            UPDATE Patient
            SET HouseId = -1,
                FamilyId = -1,
                FamilyResponsibleSus = NULL,
                FamilyResponsiblePatientId = NULL
            WHERE UserId = ?
            """,
            userId);
    }

    private static void DeleteNotes(SQLiteConnection connection, int userId)
    {
        connection.Execute("DELETE FROM Note WHERE UserId = ?", userId);
    }

    private static void MarkChanged(UserDataDeletionScope scope)
    {
        if (scope == UserDataDeletionScope.Notes)
        {
            DataChangeTracker.MarkNotesChanged();
            return;
        }

        DataChangeTracker.MarkAllChanged();
    }
}
