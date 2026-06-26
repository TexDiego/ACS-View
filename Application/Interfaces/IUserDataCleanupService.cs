using ACS_View.Domain.Enums;

namespace ACS_View.Application.Interfaces;

public interface IUserDataCleanupService
{
    Task DeleteAsync(UserDataDeletionScope scope);
}
