// Only MAUI infrastructure is substituted; production editors, commands and SQLite paths execute.
namespace ACS_View.ViewModels;
public class BaseViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public bool IsLoading { get; set; }
    public bool IsBusy { get; set; }
    public bool IsRunning { get; set; }
    protected static Task NavigateBackAsync() => Task.CompletedTask;
    protected async Task ExecuteWithLoadingAsync(Func<Task> action) { IsLoading = true; try { await action(); } finally { IsLoading = false; } }
    protected async Task ExecuteWithRunningAsync(Func<Task> action) { IsRunning = true; try { await action(); } finally { IsRunning = false; } }
    public static List<string> Alerts { get; } = [];
    public static Queue<bool> Confirmations { get; } = new();
    public static Queue<string> ActionChoices { get; } = new();
    public static string? LastRoute { get; private set; }
    public static IDictionary<string, object>? LastParameters { get; private set; }
    protected static Task DisplayAlertAsync(string title, string message, string cancel = "OK")
    { Alerts.Add(message); return Task.CompletedTask; }
    protected static Task<bool> DisplayConfirmationAsync(string title, string message, string accept, string cancel = "Cancelar")
        => Task.FromResult(Confirmations.Count > 0 && Confirmations.Dequeue());
    protected static Task<string> DisplayActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons)
        => Task.FromResult(ActionChoices.Count > 0 ? ActionChoices.Dequeue() : cancel);
    protected static Task NavigateToAsync(string route) { LastRoute = route; return Task.CompletedTask; }
    protected static Task NavigateToAsync(string route, IDictionary<string, object> args)
    { LastRoute = route; LastParameters = args; return Task.CompletedTask; }
}

// Observe the asynchronous body of MAUI commands without scheduling UI infrastructure.
public sealed class Command(Func<Task> action) : System.Windows.Input.ICommand
{
    public Task Execution { get; private set; } = Task.CompletedTask;
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => Execution = action();
}
