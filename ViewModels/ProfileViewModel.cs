using ACS_View.Application.Interfaces;
using ACS_View.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace ACS_View.ViewModels;

public partial class ProfileViewModel(IAuthService authService) : BaseViewModel
{
    [ObservableProperty] private string displayName = string.Empty;

    public ICommand OpenNotesPage => new Command(async () => await NavigateToAsync("notes"));
    public ICommand OpenImportDataPage => new Command(async () => await NavigateToAsync("importdata"));
    public ICommand OpenDataCleanupPage => new Command(async () => await NavigateToAsync("datacleanup"));
    public ICommand LoadProfileCommand => new Command(async () => await LoadProfileAsync());
    public ICommand LogoutCommand => new Command(async () => await LogoutAsync());

    private async Task LoadProfileAsync()
    {
        try
        {
            var user = await authService.GetCurrentUserAsync();
            DisplayName = user?.Username ?? "Usuario";
        }
        catch
        {
            DisplayName = "Usuario";
        }
    }

    private async Task LogoutAsync()
    {
        var confirm = await DisplayConfirmationAsync(
            "Sair da conta",
            "Deseja encerrar a sessão atual?",
            "Sair");

        if (!confirm)
        {
            return;
        }

        await authService.LogoutAsync();

        if (Microsoft.Maui.Controls.Application.Current is App app)
        {
            await app.ResetToLoginShellAsync();
        }
    }
}
