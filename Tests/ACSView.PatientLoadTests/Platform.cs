// Substitute only MAUI UI infrastructure; execute production view models and SQLite services.
global using ACS_View.Tests.Platform;
using System.Windows.Input;
using ACS_View.Application.Interfaces;
using ACS_View.Application.DTOs;

namespace ACS_View.Tests.Platform
{
    public class Command(Action execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute();
    }
    public class Command<T>(Action<T> execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute((T)parameter!);
    }
}
namespace ACS_View.ViewModels
{
    public class BaseViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public bool IsLoading { get; set; }
        public static int Navigations { get; set; }
        public static List<string> Alerts { get; } = [];
        protected static Task DisplayAlertAsync(string title, string message, string cancel = "OK")
        { Alerts.Add(message); return Task.CompletedTask; }
        protected static Task NavigateBackAsync() { Navigations++; return Task.CompletedTask; }
        protected static Task<bool> DisplayConfirmationAsync(string title, string message, string accept, string cancel = "Cancelar") => Task.FromResult(true);
        protected async Task ExecuteWithLoadingAsync(Func<Task> action)
        { IsLoading = true; try { await action(); } finally { IsLoading = false; } }
    }
}
namespace ACS_View.Views
{
    public class ConditionsPopup(ICidRepository repo) { }
    public class PregnancyPopup(PregnancyDetailsDto details) { }
    public class ParentPatientPickerPopup(IPatientService service, int? excludedId, string title) { }
}
