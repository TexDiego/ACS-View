using ACS_View.Application.Interfaces;

namespace ACS_View.Infrastructure.Services;

internal sealed class ShellDialogService : IDialogService
{
    public Task ShowAlertAsync(string title, string message, string cancel = "OK")
    {
        return Shell.Current.DisplayAlertAsync(title, message, cancel);
    }

    public Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel = "Cancelar")
    {
        return Shell.Current.DisplayAlertAsync(title, message, accept, cancel);
    }

    public Task<string> ShowActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons)
    {
        return Shell.Current.DisplayActionSheetAsync(title, cancel, destruction, buttons);
    }
}
