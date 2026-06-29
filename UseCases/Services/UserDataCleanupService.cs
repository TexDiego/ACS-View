using ACS_View.Application.Interfaces;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;
using SQLite;

namespace ACS_View.UseCases.Services;

internal sealed class UserDataCleanupService(
    IDatabaseService databaseService,
    ICurrentUserContext currentUserContext) : IUserDataCleanupService
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
    }

    private static void DeleteAll(SQLiteConnection connection, int userId)
    {
        DeletePatients(connection, userId);
        connection.Execute("DELETE FROM House WHERE UserId = ?", userId);
        DeleteNotes(connection, userId);
    }

    private static void DeletePatients(SQLiteConnection connection, int userId)
    {
        connection.Execute("DELETE FROM PatientCID WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM PatientConditions WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM PatientVaccineDose WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM Vaccines WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM Visits WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM Family WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM Patient WHERE UserId = ?", userId);
    }

    private static void DeleteHouses(SQLiteConnection connection, int userId)
    {
        connection.Execute("DELETE FROM Visits WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM Family WHERE UserId = ?", userId);
        connection.Execute("DELETE FROM House WHERE UserId = ?", userId);
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
