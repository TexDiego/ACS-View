namespace ACS_View.Application.Interfaces;

public enum SnackbarStyle { Information, Error }

public interface ISnackbarService
{
    /// <summary>Returns false immediately when another snackbar is active. Requests are never queued.</summary>
    Task<bool> ShowAsync(string message, SnackbarStyle style = SnackbarStyle.Information,
        CancellationToken cancellationToken = default);
}
