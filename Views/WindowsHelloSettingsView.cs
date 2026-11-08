#if WINDOWS
using ACS_View.Application.Interfaces;
namespace ACS_View.Views;
internal sealed class WindowsHelloSettingsView : ContentView
{
    private readonly IAuthService auth;
    private readonly IDialogService dialogs;
    private readonly IRememberedAccountService accounts;
    private readonly Label message = new();
    private readonly Entry password = new() { Placeholder = "Senha atual", IsPassword = true, MaxLength = 128, IsSpellCheckEnabled = false, IsTextPredictionEnabled = false };
    private readonly Button toggle = new() { Text = "Ativar Windows Hello", IsEnabled = false };
    private WindowsHelloStatus status = new(WindowsHelloAvailability.Unavailable, false);
    private bool busy;
    private Window? owner;
    public WindowsHelloSettingsView(IAuthService auth, IRememberedAccountService accounts, IDialogService dialogs)
    {
        this.auth = auth; this.accounts = accounts; this.dialogs = dialogs;
        var resources = Microsoft.Maui.Controls.Application.Current!.Resources;
        toggle.Style = (Style)resources["PrimaryButton"];
        var settings = new Button { Text = "Abrir configurações do Windows", Style = (Style)resources["GhostButton"] };
        settings.Clicked += async (_, _) =>
        {
            try { await Launcher.Default.OpenAsync(new Uri("ms-settings:signinoptions")); }
            catch { message.Text = "Abra Configurações > Contas > Opções de entrada no Windows."; }
        };
        Content = new VerticalStackLayout
        {
            Padding = 20, Spacing = 12, MaximumWidthRequest = 600,
            Children =
            {
                new Label { Text = "Acesso rápido com Windows Hello", Style = (Style)resources["SectionTitle"] },
                new Label { Text = "Qualquer pessoa autorizada a usar o Windows Hello desta sessão do Windows poderá entrar nesta conta do ACS View. Ative somente se confiar em todos os usuários desta sessão. A senha continua disponível. Sair preserva o acesso rápido; trocar ou recuperar a senha o invalida.", Style = (Style)resources["BodyMuted"] },
                message, password, toggle, settings
            }
        };
        toggle.Clicked += ToggleAsync;
        Loaded += (_, _) => { owner = Window; if (owner is not null) owner.Activated += Owner_Activated; };
        Unloaded += (_, _) => { if (owner is not null) owner.Activated -= Owner_Activated; owner = null; };

    }
    private async void Owner_Activated(object? sender, EventArgs e) { if (!busy) await RefreshAsync(); }
    public async Task RefreshAsync()
    {
        status = await auth.GetWindowsHelloStatusAsync();
        message.Text = status.Message;
        toggle.Text = status.Enabled ? "Desativar Windows Hello" : "Ativar Windows Hello";
        toggle.IsEnabled = !busy && (status.Enabled || status.Availability == WindowsHelloAvailability.Available);
        password.IsVisible = status.Enabled || status.Availability == WindowsHelloAvailability.Available;
    }
    private async void ToggleAsync(object? sender, EventArgs args)
    {
        if (busy) return;
        busy = true;
        toggle.IsEnabled = false;
        var enabling = !status.Enabled;
        using var interaction = (Microsoft.Maui.Controls.Application.Current as App)?.BeginExternalInteraction();
        try
        {
            if (enabling && !await dialogs.ShowConfirmationAsync("Ativar Windows Hello",
                "Esta conta será lembrada neste computador. Outras pessoas autorizadas no Windows Hello desta sessão poderão entrar nela. Deseja ativar?", "Ativar e lembrar", "Cancelar")) return;
            await auth.ConfigureWindowsHelloAsync(password.Text ?? "", enabling);
            if (enabling) await accounts.RememberCurrentAsync(true);
            await RefreshAsync();
            if (enabling && !await accounts.IsRememberedAsync((await auth.GetCurrentUserAsync())?.Id ?? 0))
                message.Text = "Windows Hello ativado, mas não foi possível lembrar a conta. Entre com senha e tente novamente.";
        }
        catch (OperationCanceledException) { message.Text = "Confirmação cancelada. Continue usando a senha."; }
        catch (Exception ex) { message.Text = ex is InvalidOperationException ? ex.Message : "Não foi possível configurar o Windows Hello. Continue usando a senha."; }
        finally { password.Text = ""; busy = false; toggle.IsEnabled = status.Enabled || status.Availability == WindowsHelloAvailability.Available; }
    }
}
#endif
