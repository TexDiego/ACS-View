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
        var shell = Shell.Current ?? throw new InvalidOperationException("Não há janela disponível para exibir a confirmação.");
        // Relationship editing is modal. Hosting its confirmation on the Shell behind
        // the editor can target the wrong native page/window on Windows.
        Page owner = shell.Navigation.ModalStack.LastOrDefault() ?? shell.CurrentPage ?? shell;
        while (owner is NavigationPage navigation) owner = navigation.CurrentPage;
        var popup = new DialogPopup(viewModel);
#if WINDOWS
        using var desktopLayout = new Platforms.Windows.DesktopPopupLayout(popup, owner.Window ?? shell.Window);
#endif
        var result = await owner.ShowPopupAsync<object>(popup, PopupConfigs.Default);

        return result is null || result.WasDismissedByTappingOutsideOfPopup
            ? viewModel.SecondaryResult
            : result.Result;
    }
}
