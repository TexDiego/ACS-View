using ACS_View.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;

namespace ACS_View.ViewModels
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        private bool _isBusy;
        private bool _isLoading;
        private bool _isRunning;
        private static IDialogService dialogService = new UnconfiguredDialogService();
        private static INavigationService navigationService = new UnconfiguredNavigationService();
        private static IMainThreadDispatcher mainThreadDispatcher = new UnconfiguredMainThreadDispatcher();

        public double PopupWidth => DefaultPopupWidth;

        public static double DefaultPopupWidth
        {
            get
            {
                try
                {
                    var info = DeviceDisplay.MainDisplayInfo;
                    var density = info.Density <= 0 ? 1 : info.Density;
                    var availableWidth = Math.Max(0, (info.Width / density) - 24);
                    return Math.Min(availableWidth, 380);
                }
                catch
                {
                    return 380;
                }
            }
        }

        internal static void ConfigureInfrastructure(
            IDialogService dialogs,
            INavigationService navigation,
            IMainThreadDispatcher mainThread)
        {
            dialogService = dialogs;
            navigationService = navigation;
            mainThreadDispatcher = mainThread;
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            set => SetProperty(ref _isRunning, value);
        }

        protected static Task DisplayAlertAsync(string title, string message, string cancel = "OK")
        {
            return dialogService.ShowAlertAsync(title, message, cancel);
        }

        protected static Task<bool> DisplayConfirmationAsync(
            string title,
            string message,
            string accept,
            string cancel = "Cancelar")
        {
            return dialogService.ShowConfirmationAsync(title, message, accept, cancel);
        }

        protected static Task<string> DisplayActionSheetAsync(
            string title,
            string cancel,
            string? destruction,
            params string[] buttons)
        {
            return dialogService.ShowActionSheetAsync(title, cancel, destruction, buttons);
        }

        protected static Task<string?> DisplayPromptAsync(
            string title,
            string message,
            string accept = "OK",
            string cancel = "Cancelar",
            string? placeholder = null,
            int maxLength = -1,
            Keyboard? keyboard = null,
            string initialValue = "")
        {
            return dialogService.ShowPromptAsync(title, message, accept, cancel, placeholder, maxLength, keyboard, initialValue);
        }

        protected static Task NavigateBackAsync()
        {
            return navigationService.GoBackAsync();
        }

        protected static Task NavigateBackAsync(IDictionary<string, object> parameters)
        {
            return navigationService.GoBackAsync(parameters);
        }

        protected static Task NavigateToAsync(string route)
        {
            return navigationService.NavigateToAsync(route);
        }

        protected static Task NavigateToAsync(string route, IDictionary<string, object> parameters)
        {
            return navigationService.NavigateToAsync(route, parameters);
        }

        protected static Task PushPageAsync(Page page)
        {
            return navigationService.PushPageAsync(page);
        }

        protected static void RunOnMainThread(Action action)
        {
            mainThreadDispatcher.Dispatch(action);
        }

        protected static void RunOnMainThread(Func<Task> action)
        {
            mainThreadDispatcher.Dispatch(action);
        }

        protected async Task ExecuteWithLoadingAsync(Func<Task> action)
        {
            await ExecuteWithStateAsync(
                action,
                setState: value => IsLoading = value);
        }

        protected async Task ExecuteWithBusyAsync(Func<Task> action)
        {
            await ExecuteWithStateAsync(
                action,
                setState: value => IsBusy = value);
        }

        protected async Task ExecuteWithRunningAsync(Func<Task> action)
        {
            await ExecuteWithStateAsync(
                action,
                setState: value => IsRunning = value);
        }

        protected static async Task ExecuteSafelyAsync(
            Func<Task> action,
            string errorMessage,
            string errorTitle = "Erro",
            string cancel = "OK")
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await DisplayAlertAsync(errorTitle, errorMessage, cancel);
            }
        }

        private static async Task ExecuteWithStateAsync(Func<Task> action, Action<bool> setState)
        {
            setState(true);

            try
            {
                await action();
            }
            finally
            {
                setState(false);
            }
        }

        private sealed class UnconfiguredDialogService : IDialogService
        {
            public Task ShowAlertAsync(string title, string message, string cancel = "OK")
            {
                throw new InvalidOperationException("IDialogService não foi configurado.");
            }

            public Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel = "Cancelar")
            {
                throw new InvalidOperationException("IDialogService não foi configurado.");
            }

            public Task<string> ShowActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons)
            {
                throw new InvalidOperationException("IDialogService não foi configurado.");
            }
            public Task<string?> ShowPromptAsync(
                string title,
                string message,
                string accept = "OK",
                string cancel = "Cancelar",
                string? placeholder = null,
                int maxLength = -1,
                Keyboard? keyboard = null,
                string initialValue = "")
            {
                throw new InvalidOperationException("IDialogService nao foi configurado.");
            }
        }
        private sealed class UnconfiguredNavigationService : INavigationService
        {
            public Task GoBackAsync()
            {
                throw new InvalidOperationException("INavigationService não foi configurado.");
            }

            public Task GoBackAsync(IDictionary<string, object> parameters)
            {
                throw new InvalidOperationException("INavigationService não foi configurado.");
            }

            public Task NavigateToAsync(string route)
            {
                throw new InvalidOperationException("INavigationService não foi configurado.");
            }

            public Task NavigateToAsync(string route, IDictionary<string, object> parameters)
            {
                throw new InvalidOperationException("INavigationService não foi configurado.");
            }

            public Task PushPageAsync(object page)
            {
                throw new InvalidOperationException("INavigationService não foi configurado.");
            }
        }

        private sealed class UnconfiguredMainThreadDispatcher : IMainThreadDispatcher
        {
            public void Dispatch(Action action)
            {
                throw new InvalidOperationException("IMainThreadDispatcher não foi configurado.");
            }

            public void Dispatch(Func<Task> action)
            {
                throw new InvalidOperationException("IMainThreadDispatcher não foi configurado.");
            }
        }
    }
}
