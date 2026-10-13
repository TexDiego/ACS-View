using ACS_View.Application.Updates;

namespace ACS_View.Application.Interfaces;

public interface IAppUpdateService
{
    Task<AppUpdate?> CheckAsync();
    Task InstallAsync(AppUpdate update, CancellationToken cancellationToken);
}
