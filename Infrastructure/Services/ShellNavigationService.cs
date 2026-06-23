using ACS_View.Application.Interfaces;

namespace ACS_View.Infrastructure.Services;

internal sealed class ShellNavigationService : INavigationService
{
    public Task GoBackAsync()
    {
        return Shell.Current.GoToAsync("..");
    }

    public Task GoBackAsync(IDictionary<string, object> parameters)
    {
        return Shell.Current.GoToAsync("..", parameters);
    }

    public Task NavigateToAsync(string route)
    {
        return Shell.Current.GoToAsync(route);
    }

    public Task NavigateToAsync(string route, IDictionary<string, object> parameters)
    {
        return Shell.Current.GoToAsync(route, parameters);
    }

    public Task PushPageAsync(object page)
    {
        if (page is not Page mauiPage)
        {
            throw new ArgumentException("O objeto informado não é uma página MAUI.", nameof(page));
        }

        return Shell.Current.Navigation.PushAsync(mauiPage);
    }
}
