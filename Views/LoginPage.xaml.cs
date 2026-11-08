using ACS_View.Application.Interfaces;
using ACS_View.Application.Updates;

namespace ACS_View.Views;

public partial class LoginPage : ContentPage
{
    private readonly IAuthService _authService;
    private readonly IDialogService _dialogService;
    private bool signingIn;
    private readonly IAppUpdateService _updates;
    private Task<bool>? _updateGate;
    private AppUpdate? _availableUpdate;
    private bool _loginAllowed;
    private bool _appeared;
    private bool _installerPending;
    private CancellationTokenSource? _download;

#if WINDOWS
    private readonly WindowsLoginAccountsView windowsAccounts;
#endif
    public LoginPage(IAuthService authService, IDialogService dialogService, IAppUpdateService updates, IRememberedAccountService rememberedAccounts)
    {
        InitializeComponent();
        _authService = authService;
        _dialogService = dialogService;
        _updates = updates;
#if WINDOWS
        windowsAccounts = new WindowsLoginAccountsView(authService, rememberedAccounts, dialogService,
            username => { UsernameEntry.Text = username; PasswordEntry.Text = string.Empty; PasswordEntry.Focus(); },
            WindowsHelloLoginAsync);
        WindowsAccountsHost.Content = windowsAccounts;
        WindowsAccountsHost.IsVisible = true;
        UsernameEntry.TextChanged += (_, _) => windowsAccounts.UsernameChanged(UsernameEntry.Text ?? "");
#endif
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        InputFocusGuard.ClearTextInputFocus(this);
        if(File.Exists(Path.Combine(FileSystem.AppDataDirectory,"profile-backup-activation.json")))
        {
            LoginForm.IsEnabled=AccountActions.IsEnabled=RestoreBackup.IsEnabled=false;
            UpdateStatus.Text="Restauração preparada. Feche completamente e abra o ACS View para ativar o perfil e entrar com a senha original.";
            UpdateStatus.IsVisible=true;
            return;
        }
        if (_appeared) return;
        _appeared = true;
#if WINDOWS
        await windowsAccounts.RefreshAsync();
#endif
        if (!await PrepareForLoginAsync() || Window is null) return;

        var biometricStatus=await _authService.GetBiometricStatusAsync(); // initializes restored settings before deciding on automatic prompts
        if(Preferences.Get("BackupRequirePasswordOnce","")=="1")
        {
            Preferences.Remove("BackupRequirePasswordOnce");
            return;
        }
        if (biometricStatus is { Enabled: true })
        {
            await BiometricLogin();
        }
    }

    public Task<bool> PrepareForLoginAsync() => _updateGate ??= CheckUpdateAsync();

    private async Task<bool> CheckUpdateAsync()
    {
        _availableUpdate = await _updates.CheckAsync();
        UpdateCard.IsVisible = false;
        UpdateStatus.IsVisible = false;
        if (_availableUpdate is { } update)
        {
            UpdateMessage.Text = $"A versão {update.Version} está disponível. " + (update.Required
                ? "Esta atualização é necessária para entrar."
                : "Você pode instalar agora ou continuar com a versão atual.");
            UpdateCard.IsVisible = true;
            SkipUpdate.IsVisible = !update.Required;
            var install = await _dialogService.ShowConfirmationAsync(
                update.Required ? "Atualização necessária" : "Atualização disponível",
                UpdateMessage.Text, "Instalar", update.Required ? "Fechar" : "Agora não");
            if (install) await InstallAvailableUpdateAsync();
            if (update.Required) return false;
            // Keep login manual after opening the Android installer.
            SetLoginAllowed(true);
            return !install;
        }
        SetLoginAllowed(true);
        return true;
    }

    private void SetLoginAllowed(bool allowed)
    {
        _loginAllowed = allowed && _download is null && !_installerPending;
        LoginForm.IsEnabled = _loginAllowed;
        AccountActions.IsEnabled = _loginAllowed;
    }

    private async void InstallUpdate_Clicked(object sender, EventArgs e) => await InstallAvailableUpdateAsync();

    private async Task InstallAvailableUpdateAsync()
    {
        if (_availableUpdate is null || _download is not null || _installerPending || signingIn) return;
        _download = new CancellationTokenSource();
        SetLoginAllowed(false);
        InstallUpdate.IsEnabled = SkipUpdate.IsEnabled = false;
        CancelDownload.IsVisible = true;
        UpdateStatus.Text = "Atualização em andamento.";
        UpdateStatus.IsVisible = true;
        UpdateProgressPanel.IsVisible = true;
        UpdateProgress.Progress = 0;
        UpdateProgressText.Text = "Preparando atualização…";
        var progress = new Progress<AppUpdateProgress>(value =>
        {
            if (_download is null) return;
            UpdateProgress.Progress = value.Progress;
            UpdateProgressText.Text = value.Message;
        });
        var interaction = (Microsoft.Maui.Controls.Application.Current as App)?.BeginExternalInteraction();
        var window = Window;
        var installerOpened = false;
        var returned = false;
        EventHandler? resumed = null;
        resumed = (_, _) =>
        {
            // App switches during download must not release the installer/login gate.
            if (_download is not null && !installerOpened) return;
            returned = true;
            if (installerOpened)
            {
                _installerPending = false;
                InstallUpdate.IsEnabled = SkipUpdate.IsEnabled = true;
                UpdateProgressPanel.IsVisible = false;
                UpdateStatus.Text = "Se a instalação não foi concluída, toque em Instalar atualização para tentar novamente.";
                SetLoginAllowed(_availableUpdate?.Required != true);
            }
            interaction?.Dispose();
            if (window is not null) window.Resumed -= resumed;
        };
        if (window is not null) window.Resumed += resumed;
        try
        {
            await _updates.InstallAsync(_availableUpdate, _download.Token, progress);
            installerOpened = true;
            _installerPending = window is not null && !returned;
            UpdateProgress.Progress = 1;
            UpdateProgressText.Text = "Arquivo pronto. Confirme a instalação na tela do Android.";
            UpdateStatus.Text = "Confirme a instalação no Android. Se cancelar, toque em Instalar novamente.";
        }
        catch (OperationCanceledException)
        {
            UpdateStatus.Text = "Download cancelado ou tempo limite atingido. Você pode tentar novamente.";
        }
        catch (Exception ex)
        {
            UpdateStatus.Text = "Não foi possível instalar a atualização.";
            await _dialogService.ShowAlertAsync("Atualização", ex is InvalidOperationException ? ex.Message : "Falha no download. Verifique sua conexão e tente novamente.");
        }
        finally
        {
            if (!installerOpened || window is null)
            {
                interaction?.Dispose();
                if (window is not null) window.Resumed -= resumed;
            }
            _download.Dispose();
            _download = null;
            InstallUpdate.IsEnabled = SkipUpdate.IsEnabled = !_installerPending;
            CancelDownload.IsVisible = false;
            if (!_installerPending) UpdateProgressPanel.IsVisible = false;
            SetLoginAllowed(!_availableUpdate.Required);
        }
    }

    private void SkipUpdate_Clicked(object sender, EventArgs e)
    {
        if (_availableUpdate?.Required == true || _download is not null || _installerPending) return;
        UpdateCard.IsVisible = UpdateStatus.IsVisible = false;
        SetLoginAllowed(true);
    }

    private void CancelDownload_Clicked(object sender, EventArgs e) => _download?.Cancel();

    protected override void OnDisappearing()
    {
        InputFocusGuard.ClearTextInputFocus(this);
        base.OnDisappearing();
    }

    private async void Btn_Login_Clicked(object sender, EventArgs e)
    {
        if (signingIn || !_loginAllowed) return;
        signingIn = true;
        if (sender is Button button) button.IsEnabled = false;
        try
        {
            LoginForm.IsEnabled = AccountActions.IsEnabled = false;
            await _authService.LoginAsync(UsernameEntry.Text ?? string.Empty, PasswordEntry.Text ?? string.Empty);
#if WINDOWS
            await windowsAccounts.RememberAfterLoginAsync();
#endif

            await CompleteLoginNavigationAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Erro", ex.Message, "Voltar");
        }
        finally
        {
            signingIn = false;
            if (sender is Button loginButton) loginButton.IsEnabled = true;
            SetLoginAllowed(_loginAllowed);
        }
    }

    private async Task CompleteLoginNavigationAsync()
    {
        PasswordEntry.Text = string.Empty;
        if (Window is null) { await _authService.LogoutAsync(); return; }
        if (Microsoft.Maui.Controls.Application.Current is App app)
        {
            await app.ResetToAuthenticatedShellAsync();
            var user = await _authService.GetCurrentUserAsync();
            if (string.IsNullOrEmpty(user?.RecoveryCodeHash)) await Shell.Current.GoToAsync("accountsecurity");
        }
    }
#if WINDOWS
    private async Task WindowsHelloLoginAsync(int userId)
    {
        if (signingIn || !_loginAllowed) return;
        signingIn = true;
        LoginForm.IsEnabled = AccountActions.IsEnabled = false;
        using var interaction = (Microsoft.Maui.Controls.Application.Current as App)?.BeginExternalInteraction();
        try
        {
            await _authService.LoginWithWindowsHelloAsync(userId);
            await windowsAccounts.RememberAfterLoginAsync();
            await CompleteLoginNavigationAsync();
        }
        catch (OperationCanceledException) { PasswordEntry.Focus(); }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Windows Hello", ex is InvalidOperationException ? ex.Message : "Não foi possível autenticar. Use a senha.", "OK");
        }
        finally
        {
            signingIn = false;
            SetLoginAllowed(_loginAllowed);
            await windowsAccounts.RefreshAsync();
            PasswordEntry.Focus();
        }
    }
#endif

    private async void RegisterUser_Clicked(object sender, EventArgs e)
    {
        if (!_loginAllowed || signingIn) return;
        await Shell.Current.GoToAsync("registration");
    }

    private async void RestoreBackup_Clicked(object sender, EventArgs e)
    {
        if(signingIn || _download is not null || _installerPending)return;
        await Shell.Current.GoToAsync("backup");
    }

    private async Task BiometricLogin()
    {
        if (signingIn || !_loginAllowed) return;
        signingIn = true;
        Btn_Login.IsEnabled = false;
        using var interaction = (Microsoft.Maui.Controls.Application.Current as App)?.BeginExternalInteraction();
        try
        {
            await _authService.LoginWithBiometricsAsync();
            PasswordEntry.Text = string.Empty;
            if (Microsoft.Maui.Controls.Application.Current is App app)
                await app.ResetToAuthenticatedShellAsync();
        }
        catch (OperationCanceledException) { PasswordEntry.Focus(); }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Biometria", ex is InvalidOperationException ? ex.Message : "Entre com a senha da conta.", "OK");
        }
        finally
        {
            signingIn = false;
            Btn_Login.IsEnabled = true;
        }
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        if (!_loginAllowed || signingIn) return;
        await Shell.Current.GoToAsync("forgotpassword");
    }

    private void UsernameEntry_Completed(object sender, EventArgs e)
    {
        PasswordEntry.Focus();
    }

    private void PasswordEntry_Completed(object sender, EventArgs e)
    {
        Btn_Login_Clicked(Btn_Login, EventArgs.Empty);
    }
}
