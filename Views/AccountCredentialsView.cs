using ACS_View.Application.Interfaces;
using ACS_View.Application.Security;

namespace ACS_View.Views;

internal enum AccountFormKind { Registration, Recovery, Security }

// Shared form keeps password confirmation and one-time recovery-code handling consistent.
internal sealed class AccountCredentialsView : ContentView
{
    private readonly Entry username = new() { Placeholder = "Nome de usuário", MaxLength = 64, IsSpellCheckEnabled = false, IsTextPredictionEnabled = false };
    private readonly Entry currentPassword = PasswordEntry("Senha atual");
    private readonly Entry recoveryCode = new() { Placeholder = "Código de recuperação", MaxLength = 64, IsSpellCheckEnabled = false, IsTextPredictionEnabled = false, IsPassword = true };
    private readonly Entry password = PasswordEntry("Nova senha");
    private readonly Entry confirmation = PasswordEntry("Repita a nova senha");
    private readonly VerticalStackLayout form = new() { Spacing = 14 };
    private readonly VerticalStackLayout codePanel = new() { Spacing = 14, IsVisible = false };
    private readonly Label status = new() { IsVisible = false };
    private readonly Editor codeDisplay = new() { IsReadOnly = true, AutoSize = EditorAutoSizeOption.TextChanges, FontSize = 20 };
    private readonly Button submit;
    private readonly Button done = new() { Text = "Concluir", IsEnabled = false };
    private readonly CheckBox saved = new();
    private bool busy;
    private IDisposable? codeInteraction;
    public bool ShowingCode => codePanel.IsVisible;
    public event EventHandler? RecoveryCodeShown;
    public void UseParentScrolling()
    {
        if (Content is ScrollView scroll)
        {
            var stack = scroll.Content;
            scroll.Content = null;
            Content = stack;
        }
    }

    public AccountCredentialsView(ContentPage page, IAuthService auth, AccountFormKind kind, Func<Task> completed)
    {
        var title = kind switch
        {
            AccountFormKind.Registration => "Crie sua conta local",
            AccountFormKind.Recovery => "Recupere sua conta",
            _ => "Proteja sua conta"
        };
        form.Add(StyledLabel(title, "SectionTitle"));
        form.Add(StyledLabel(kind == AccountFormKind.Recovery
            ? "Informe o usuário e o código guardado ao criar ou proteger a conta. Escolha sua própria senha nova."
            : "Use uma frase de 15 a 128 caracteres, com várias palavras. Espaços são aceitos; não é preciso decorar combinações de símbolos.", "BodyMuted"));
        if (kind != AccountFormKind.Security) form.Add(username);
        if (kind == AccountFormKind.Security)
        {
            form.Add(StyledLabel("Confirme a senha atual e escolha uma senha longa. Você pode repetir a atual se ela já tiver pelo menos 15 caracteres. Um novo código substituirá o anterior.", "BodyMuted"));
            form.Add(currentPassword);
        }
        if (kind == AccountFormKind.Recovery) form.Add(recoveryCode);
        form.Add(password);
        form.Add(confirmation);
        var reveal = new CheckBox();
        reveal.CheckedChanged += (_, e) => currentPassword.IsPassword = password.IsPassword = confirmation.IsPassword = recoveryCode.IsPassword = !e.Value;
        form.Add(CheckRow(reveal, "Mostrar senhas e código"));
        form.Add(StyledLabel("A conta e a recuperação funcionam somente neste aparelho. O código não restaura dados de um aplicativo desinstalado. Contas antigas precisam configurar um código usando a senha atual.", "BodyMuted"));
        submit = new Button { Text = kind switch { AccountFormKind.Registration => "Criar conta", AccountFormKind.Recovery => "Redefinir senha", _ => "Salvar senha e gerar código" } };
        submit.Style = (Style)Microsoft.Maui.Controls.Application.Current!.Resources["Buttons"];
        form.Add(submit);
        form.Add(status);
        submit.Clicked += async (_, _) =>
        {
            if (busy) return;
            busy = true;
            submit.IsEnabled = false;
            status.IsVisible = false;
            try
            {
                if (!string.Equals(password.Text, confirmation.Text, StringComparison.Ordinal))
                    throw new InvalidOperationException("As senhas não coincidem. Confira a confirmação.");
                AccountSecurity.ValidatePassword(password.Text ?? string.Empty);
                var code = kind switch
                {
                    AccountFormKind.Registration => await auth.RegisterAsync(username.Text ?? "", password.Text ?? ""),
                    AccountFormKind.Recovery => await auth.ResetPasswordAsync(username.Text ?? "", recoveryCode.Text ?? "", password.Text ?? ""),
                    _ => await auth.ConfigureRecoveryAsync(currentPassword.Text ?? "", password.Text ?? "")
                };
                currentPassword.Text = password.Text = confirmation.Text = recoveryCode.Text = string.Empty;
                reveal.IsChecked = false;
                form.IsVisible = false;
                codeDisplay.Text = code;
                codePanel.IsVisible = true;
                RecoveryCodeShown?.Invoke(this, EventArgs.Empty);
                // Let the user switch to a password manager without discarding the only copy.
                codeInteraction = (Microsoft.Maui.Controls.Application.Current as App)?.BeginExternalInteraction();
                Shell.SetBackButtonBehavior(page, new BackButtonBehavior { IsEnabled = false, IsVisible = false });
            }
            catch (Exception ex)
            {
                status.Text = ex is InvalidOperationException ? ex.Message : "Não foi possível salvar com segurança. Tente novamente.";
                status.IsVisible = true;
            }
            finally { busy = false; submit.IsEnabled = true; }
        };
        codePanel.Add(StyledLabel("Guarde seu código de recuperação", "SectionTitle"));
        codePanel.Add(StyledLabel("Este código aparece uma única vez. Anote e guarde em um lugar seguro, separado do aparelho. Quem tiver o código poderá trocar sua senha. Ele será substituído quando for usado ou quando você gerar outro.", "BodyMuted"));
        codePanel.Add(codeDisplay);
        saved.CheckedChanged += (_, e) => done.IsEnabled = e.Value;
        codePanel.Add(CheckRow(saved, "Guardei o código em um lugar seguro"));
        done.Style = submit.Style;
        codePanel.Add(done);
        done.Clicked += async (_, _) =>
        {
            if (!saved.IsChecked || busy) return;
            busy = true;
            done.IsEnabled = false;
            try
            {
                await completed();
                codeDisplay.Text = string.Empty;
                codePanel.IsVisible = false;
                codeInteraction?.Dispose();
                codeInteraction = null;
            }
            catch (Exception)
            {
                await page.DisplayAlertAsync("Código guardado", "Não foi possível concluir a navegação. Tente tocar em Concluir novamente.", "OK");
            }
            finally { busy = false; done.IsEnabled = saved.IsChecked; }
        };
        var stack = new VerticalStackLayout { Padding = 20, Spacing = 16, MaximumWidthRequest = 600, Children = { form, codePanel } };
        Content = new ScrollView { Content = stack };
    }

    private static Entry PasswordEntry(string placeholder) => new()
    {
        Placeholder = placeholder, IsPassword = true, MaxLength = 128,
        IsSpellCheckEnabled = false, IsTextPredictionEnabled = false
    };
    private static Grid CheckRow(CheckBox checkBox, string text)
    {
        var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) } };
        row.Add(checkBox);
        row.Add(new Label { Text = text, VerticalOptions = LayoutOptions.Center }, 1);
        return row;
    }
    private static Label StyledLabel(string text, string style) => new()
    {
        Text = text, Style = (Style)Microsoft.Maui.Controls.Application.Current!.Resources[style]
    };
}
