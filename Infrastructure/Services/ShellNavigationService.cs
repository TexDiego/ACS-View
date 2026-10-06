using ACS_View.Application.Interfaces;
using ACS_View.Views;

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

    public async Task NavigateToAsync(string route, IDictionary<string, object> parameters)
    {
        var shell = Shell.Current;
        if (route == "families" &&
            parameters.TryGetValue("id", out var id) &&
            int.TryParse(id?.ToString(), out var houseId) &&
            houseId > 0 &&
            await TryShowExistingFamiliesAsync(shell, houseId))
        {
            return;
        }

        await shell.GoToAsync(route, parameters);
    }

    private static async Task<bool> TryShowExistingFamiliesAsync(Shell shell, int houseId)
    {
        var currentSection = shell.CurrentItem?.CurrentItem;
        var sections = shell.Items
            .SelectMany(item => item.Items.Select(section => (Item: item, Section: section)))
            .OrderByDescending(entry => ReferenceEquals(entry.Section, currentSection));

        foreach (var (item, section) in sections)
        {
            // Each tab retains its own stack. Match the residence, not the page title.
            var page = section.Navigation.NavigationStack
                .OfType<FamiliesPage>()
                .LastOrDefault(candidate => candidate.HouseId == houseId);
            if (page is null)
            {
                continue;
            }

            // Selecting the owning tab restores its stack without pushing another page.
            item.CurrentItem = section;
            shell.CurrentItem = item;

            while (section.Navigation.NavigationStack.LastOrDefault() != page)
            {
                var previousCount = section.Navigation.NavigationStack.Count;
                await section.Navigation.PopAsync(animated: false);
                if (section.Navigation.NavigationStack.Count >= previousCount)
                {
                    throw new InvalidOperationException("Não foi possível retornar à página de famílias existente.");
                }
            }

            return true;
        }

        return false;
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
