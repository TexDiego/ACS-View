using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace ACS_View.ViewModels;

// Only the MAUI base and command are replaced; validation uses the production view model.
public class BaseViewModel : ObservableObject { }

public sealed class Command<T>(Action<T?> execute) : ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => execute((T?)parameter);
}
