namespace ACS_View.Application.Interfaces;

public interface IAppStartupService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
