using ACS_View.Application.Interfaces;

namespace ACS_View.Views;

internal sealed class BiometricSettingsView : ContentView
{
    private readonly Label message = new();
    private readonly Entry password = new()
    {
        Placeholder = "Senha atual para configurar a biometria", IsPassword = true, MaxLength = 128,
        IsSpellCheckEnabled = false, IsTextPredictionEnabled = false
    };
    private readonly Button toggle = new() { Text = "Ativar biometria", IsEnabled = false };
    private readonly IAuthService auth;
    private BiometricStatus status = new(false, false);
    private bool busy;

    public BiometricSettingsView(IAuthService authService)
    {
        auth = authService;
        toggle.Style = (Style)Microsoft.Maui.Controls.Application.Current!.Resources["Buttons"];
        Content = new VerticalStackLayout
        {
            Padding = 20, Spacing = 12, MaximumWidthRequest = 600,
            Children =
            {
                new Label { Text = "Biometria neste aparelho", Style = (Style)Microsoft.Maui.Controls.Application.Current.Resources["SectionTitle"] },
                new Label { Text = "Ative para confirmar sua identidade ao abrir ou retornar ao app. Todas as biometrias cadastradas no aparelho poderão desbloquear esta conta. Sair da conta, trocar a senha ou recuperar a conta remove a ativação.", Style = (Style)Microsoft.Maui.Controls.Application.Current.Resources["BodyMuted"] },
                message, password, toggle
            }
        };
        toggle.Clicked += Toggle_Clicked;
    }

    public async Task RefreshAsync()
    {
        status = await auth.GetBiometricStatusAsync();
        message.Text = status.Enabled ? "Biometria ativada para esta conta."
            : status.Available ? "Biometria disponível. Confirme a senha para ativar."
            : "Ativação disponível no Android 11 ou superior, com uma biometria segura cadastrada. Neste aparelho, use a senha.";
        toggle.Text = status.Enabled ? "Desativar biometria" : "Ativar biometria";
        toggle.IsEnabled = !busy && (status.Available || status.Enabled);
        password.IsVisible = status.Available || status.Enabled;
    }

    private async void Toggle_Clicked(object? sender, EventArgs e)
    {
        if (busy) return;
        busy = true;
        toggle.IsEnabled = false;
        using var interaction = (Microsoft.Maui.Controls.Application.Current as App)?.BeginExternalInteraction();
        try
        {
            await auth.ConfigureBiometricsAsync(password.Text ?? "", !status.Enabled);
            password.Text = string.Empty;
            await RefreshAsync();
        }
        catch (OperationCanceledException) { message.Text = "Confirmação cancelada. A senha continua disponível."; }
        catch (Exception ex) { message.Text = ex is InvalidOperationException ? ex.Message : "Não foi possível configurar a biometria. Tente novamente."; }
        finally { busy = false; toggle.IsEnabled = status.Available || status.Enabled; }
    }
}
