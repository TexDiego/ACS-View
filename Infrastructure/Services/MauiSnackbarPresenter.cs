using ACS_View.Application.Interfaces;
using ACS_View.Controls;

namespace ACS_View.Infrastructure.Services;

internal sealed class MauiSnackbarPresenter : ISnackbarPresenter
{
    public Task ShowUntilDismissedAsync(string message, SnackbarStyle style, CancellationToken cancellationToken)
        => MainThread.InvokeOnMainThreadAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var root = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page;
            // Toolkit popups are modal ContentPages. Place the message above their content.
            var page = root?.Navigation.ModalStack.LastOrDefault() as ContentPage
                ?? (root as Shell)?.CurrentPage as ContentPage
                ?? (root as NavigationPage)?.CurrentPage as ContentPage
                ?? root as ContentPage
                ?? throw new InvalidOperationException("Não há página disponível para exibir o snackbar.");
            var originalContent = page.Content;
            // Keep existing Grid instances intact: PopupPage relies on the concrete
            // type of its Content during close and outside-tap handling.
            var host = originalContent as Grid ?? new Grid();
            var needsWrapper = originalContent is not Grid;
            var snackbar = new AppSnackbar(message, style);
            Grid.SetRowSpan(snackbar, Math.Max(1, host.RowDefinitions.Count));
            Grid.SetColumnSpan(snackbar, Math.Max(1, host.ColumnDefinitions.Count));
            var disappeared = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void OnDisappearing(object? sender, EventArgs args) => disappeared.TrySetResult();

            page.Disappearing += OnDisappearing;
            try
            {
                if (needsWrapper)
                {
                    page.Content = null;
                    if (originalContent is not null) host.Children.Add(originalContent);
                    page.Content = host;
                }
                host.Children.Add(snackbar);
                SemanticScreenReader.Default.Announce(message);
                await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(4), cancellationToken), disappeared.Task);
                cancellationToken.ThrowIfCancellationRequested();
            }
            finally
            {
                page.Disappearing -= OnDisappearing;
                host.Children.Remove(snackbar);
                if (needsWrapper)
                {
                    if (originalContent is not null) host.Children.Remove(originalContent);
                    // Navigation or another UI update may have replaced the content meanwhile.
                    if (ReferenceEquals(page.Content, host)) page.Content = originalContent;
                }
            }
        });
}
