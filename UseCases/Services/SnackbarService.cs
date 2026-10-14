using ACS_View.Application.Interfaces;

namespace ACS_View.UseCases.Services;

public sealed class SnackbarService(ISnackbarPresenter presenter) : ISnackbarService
{
    private int active;

    public async Task<bool> ShowAsync(string message, SnackbarStyle style = SnackbarStyle.Information,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message) || cancellationToken.IsCancellationRequested ||
            Interlocked.CompareExchange(ref active, 1, 0) != 0)
            return false;

        try
        {
            await presenter.ShowUntilDismissedAsync(message, style, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao exibir snackbar: {ex}");
            return false;
        }
        finally
        {
            Interlocked.Exchange(ref active, 0);
        }
    }
}
