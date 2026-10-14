namespace ACS_View.Application.Interfaces;

public interface ISnackbarPresenter
{
    /// <summary>Completes only after the snackbar has been dismissed.</summary>
    Task ShowUntilDismissedAsync(string message, SnackbarStyle style, CancellationToken cancellationToken);
}
