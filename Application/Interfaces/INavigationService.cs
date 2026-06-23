namespace ACS_View.Application.Interfaces;

public interface INavigationService
{
    Task GoBackAsync();
    Task GoBackAsync(IDictionary<string, object> parameters);
    Task NavigateToAsync(string route);
    Task NavigateToAsync(string route, IDictionary<string, object> parameters);
    Task PushPageAsync(object page);
}
