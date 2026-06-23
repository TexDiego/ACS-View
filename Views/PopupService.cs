using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using CommunityToolkit.Maui.Extensions;

namespace ACS_View.Views;

internal sealed class PopupService : IPopupService
{
    public async Task<PopupResultDto<T>> ShowAsync<T>(object popup, IDictionary<string, object>? parameters = null)
    {
        if (popup is not CommunityToolkit.Maui.Views.Popup mauiPopup)
        {
            throw new ArgumentException("O objeto informado não é um popup MAUI.", nameof(popup));
        }

        var result = parameters is null
            ? await Shell.Current.ShowPopupAsync<T>(mauiPopup, PopupConfigs.Default)
            : await Shell.Current.ShowPopupAsync<T>(mauiPopup, PopupConfigs.Default, parameters);

        return new PopupResultDto<T>
        {
            WasDismissed = result.WasDismissedByTappingOutsideOfPopup,
            Result = result.Result
        };
    }

    public async Task ShowAsync(object popup)
    {
        if (popup is not CommunityToolkit.Maui.Views.Popup mauiPopup)
        {
            throw new ArgumentException("O objeto informado não é um popup MAUI.", nameof(popup));
        }

        await Shell.Current.ShowPopupAsync(mauiPopup, PopupConfigs.Default);
    }
}
