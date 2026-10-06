using ACS_View.Application.Interfaces;
using ACS_View.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace ACS_View.ViewModels;

public partial class ProfileViewModel(IAuthService authService) : BaseViewModel
{
    [ObservableProperty] private string displayName = string.Empty;
    [ObservableProperty] private string securityStatus = string.Empty;

    public ICommand OpenNotesPage => new Command(async () => await NavigateToAsync("notes"));
    public ICommand OpenImportDataPage => new Command(async () => await NavigateToAsync("importdata"));
    public ICommand OpenDataCleanupPage => new Command(async () => await NavigateToAsync("datacleanup"));
    public ICommand OpenAccountSecurityPage => new Command(async () => await NavigateToAsync("accountsecurity"));
    public ICommand LoadProfileCommand => new Command(async () => await LoadProfileAsync());
    public ICommand LogoutCommand => new Command(async () => await LogoutAsync());

    private async Task LoadProfileAsync()
    {
        try
        {
            var user = await authService.GetCurrentUserAsync();
            DisplayName = user?.Username ?? "Usuario";
            SecurityStatus = string.IsNullOrEmpty(user?.RecoveryCodeHash)
                ? "Configure seu código de recuperação com a senha atual. A pergunta de segurança foi desativada."
                : "Gerencie a senha, o código de recuperação e a biometria neste aparelho.";
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
            "Deseja encerrar a sessão atual? A biometria, se ativada, será desativada neste aparelho.",
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
