using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;

namespace ACS_View.ViewModels
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        private bool _isBusy;
        private bool _isLoading;
        private bool _isRunning;

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
            return Shell.Current.DisplayAlertAsync(title, message, cancel);
        }

        protected static Task<bool> DisplayConfirmationAsync(
            string title,
            string message,
            string accept,
            string cancel = "Cancelar")
        {
            return Shell.Current.DisplayAlertAsync(title, message, accept, cancel);
        }

        protected static Task<string> DisplayActionSheetAsync(
            string title,
            string cancel,
            string? destruction,
            params string[] buttons)
        {
            return Shell.Current.DisplayActionSheetAsync(title, cancel, destruction, buttons);
        }

        protected static Task NavigateBackAsync()
        {
            return Shell.Current.GoToAsync("..");
        }

        protected static Task NavigateBackAsync(IDictionary<string, object> parameters)
        {
            return Shell.Current.GoToAsync("..", parameters);
        }

        protected static Task NavigateToAsync(string route)
        {
            return Shell.Current.GoToAsync(route);
        }

        protected static Task NavigateToAsync(string route, IDictionary<string, object> parameters)
        {
            return Shell.Current.GoToAsync(route, parameters);
        }

        protected static Task PushPageAsync(Page page)
        {
            return Shell.Current.Navigation.PushAsync(page);
        }

        protected static void RunOnMainThread(Action action)
        {
            MainThread.BeginInvokeOnMainThread(action);
        }

        protected static void RunOnMainThread(Func<Task> action)
        {
            MainThread.BeginInvokeOnMainThread(async () => await action());
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
    }
}
