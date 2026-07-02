using ACS_View.Application.Interfaces;
using ACS_View.ViewModels;
using ACS_View.Views;
using CommunityToolkit.Maui.Extensions;

namespace ACS_View.Infrastructure.Services;

internal sealed class ShellDialogService : IDialogService
{
    public async Task ShowAlertAsync(string title, string message, string cancel = "OK")
    {
        await ShowDialogAsync(DialogPopupViewModel.Alert(title, message, cancel));
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel = "Cancelar")
    {
        var result = await ShowDialogAsync(DialogPopupViewModel.Confirmation(title, message, accept, cancel));
        return result is true;
    }

    public async Task<string> ShowActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons)
    {
        var result = await ShowDialogAsync(DialogPopupViewModel.ActionSheet(title, cancel, destruction, buttons));
        return result?.ToString() ?? cancel;
    }

    public async Task<string?> ShowPromptAsync(
        string title,
        string message,
        string accept = "OK",
        string cancel = "Cancelar",
        string? placeholder = null,
        int maxLength = -1,
        Keyboard? keyboard = null,
        string initialValue = "")
    {
        var result = await ShowDialogAsync(DialogPopupViewModel.Prompt(
            title,
            message,
            accept,
            cancel,
            placeholder,
            maxLength,
            keyboard,
            initialValue));

        return result?.ToString();
    }

    private static async Task<object?> ShowDialogAsync(DialogPopupViewModel viewModel)
    {
        var popup = new DialogPopup(viewModel);
        var result = await Shell.Current.ShowPopupAsync<object>(popup, PopupConfigs.Default);

        return result is null || result.WasDismissedByTappingOutsideOfPopup
            ? viewModel.SecondaryResult
            : result.Result;
    }
}
