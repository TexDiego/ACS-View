namespace ACS_View.Application.Interfaces;

public interface IDialogService
{
    Task ShowAlertAsync(string title, string message, string cancel = "OK");
    Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel = "Cancelar");
    Task<string> ShowActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons);
}
