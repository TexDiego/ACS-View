#if WINDOWS
using ACS_View.Application.Interfaces;
namespace ACS_View.Views;

internal sealed class WindowsLoginAccountsView : ContentView
{
    private readonly IAuthService auth;
    private readonly IRememberedAccountService accounts;
    private readonly IDialogService dialogs;
    private readonly Action<string> select;
    private readonly Func<int, Task> login;
    private readonly VerticalStackLayout section = new() { Spacing = 8, IsVisible = false };
    private readonly CollectionView list = new() { SelectionMode = SelectionMode.Single, HeightRequest = 180 };
    private readonly CheckBox remember = new();
    private readonly Button hello = new() { Text = "Entrar com Windows Hello", IsVisible = false };
    private readonly Button another = new() { Text = "Usar outra conta" };
    private int? selectedId;
    private string selectedUsername = "";
    private bool busy;
    private int selectionVersion;
    public WindowsLoginAccountsView(IAuthService auth, IRememberedAccountService accounts, IDialogService dialogs, Action<string> select, Func<int, Task> login)
    {
        this.auth = auth; this.accounts = accounts; this.dialogs = dialogs; this.select = select; this.login = login;
        var resources = Microsoft.Maui.Controls.Application.Current!.Resources;
        section.Children.Add(new Label { Text = "Contas neste dispositivo", Style = (Style)resources["SectionTitle"] });
        section.Children.Add(list);
        another.Style = (Style)resources["GhostButton"];
        section.Children.Add(another);
        list.SelectionChanged += async (_, e) =>
        {
            if (!busy && e.CurrentSelection.FirstOrDefault() is RememberedAccount account) await SelectAccountAsync(account);
        };
        list.ItemTemplate = new DataTemplate(() =>
        {
            var choose = new Button { Style = (Style)resources["GhostButton"], HorizontalOptions = LayoutOptions.Fill };
            choose.SetBinding(Button.TextProperty, nameof(RememberedAccount.Username));
            SemanticProperties.SetHint(choose, "Selecionar conta e preencher o usuário. A autenticação ainda será necessária.");
            var badge = new Label { Text = "Windows Hello", FontSize = 12, VerticalOptions = LayoutOptions.Center };
            badge.SetBinding(IsVisibleProperty, nameof(RememberedAccount.WindowsHelloEnabled));
            var forget = new Button { Text = "Esquecer", Style = (Style)resources["GhostButton"] };
            choose.Clicked += async (_, _) =>
            {
                if (busy || choose.BindingContext is not RememberedAccount account) return;
                if (Equals(list.SelectedItem, account)) await SelectAccountAsync(account);
                else list.SelectedItem = account;
            };
            forget.Clicked += async (_, _) =>
            {
                if (busy || forget.BindingContext is not RememberedAccount account) return;
                busy = true; IsEnabled = false;
                try
                {
                    await accounts.ForgetAsync(account.UserId);
                    if (selectedId == account.UserId) ClearSelection();
                    await RefreshAsync();
                }
                catch { await dialogs.ShowAlertAsync("Contas neste dispositivo", "Não foi possível esquecer a conta. Tente novamente. Sua conta e seus dados foram preservados.", "OK"); }
                finally { busy = false; IsEnabled = true; }
            };
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection(new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)), Padding = 4, ColumnSpacing = 8 };
            grid.Add(new Image
            {
                Source = new ACS_View.Controls.ThemedIconExtension { Icon = MauiIcons.Fluent.FluentIcons.PersonCircle24, IconSize = 32 }.ProvideValue(null!),
                WidthRequest = 32, HeightRequest = 32, VerticalOptions = LayoutOptions.Center
            }, 0);
            grid.Add(new VerticalStackLayout { Children = { choose, badge } }, 1);
            grid.Add(forget, 2);
            return new Border { Style = (Style)resources["CardBorder"], Content = grid, Padding = 4, Margin = new Thickness(0, 2) };
        });
        hello.Style = (Style)resources["PrimaryButton"];
        hello.Clicked += async (_, _) => { if (selectedId is int id && !busy) await login(id); };
        another.Clicked += (_, _) => { if (!busy) ClearSelection(); };
        var consent = new HorizontalStackLayout { Spacing = 8, Children = { remember, new Label { Text = "Lembrar esta conta neste computador", VerticalOptions = LayoutOptions.Center } } };
        SemanticProperties.SetDescription(remember, "Lembrar esta conta neste computador");
        Content = new VerticalStackLayout { Spacing = 10, Children = { section, consent, hello } };
    }
    private async Task SelectAccountAsync(RememberedAccount account)
    {
        var version = ++selectionVersion;
        selectedId = account.UserId; selectedUsername = account.Username;
        hello.IsVisible = false;
        select(account.Username); remember.IsChecked = true;
        var status = await auth.GetWindowsHelloStatusAsync(account.UserId);
        if (version == selectionVersion) hello.IsVisible = status.Enabled && status.Availability == WindowsHelloAvailability.Available;
    }
    public async Task RefreshAsync()
    {
        var items = await accounts.GetAccountsAsync();
        list.ItemsSource = items;
        list.HeightRequest = Math.Min(240, items.Count * 90);
        section.IsVisible = items.Count > 0;
    }
    public void UsernameChanged(string username)
    {
        if (selectedId is not null && username != selectedUsername)
        {
            ++selectionVersion; selectedId = null; hello.IsVisible = false; remember.IsChecked = false;
        }
    }
    private void ClearSelection()
    {
        ++selectionVersion; selectedId = null; selectedUsername = ""; list.SelectedItem = null;
        hello.IsVisible = false; remember.IsChecked = false; select("");
    }
    public Task RememberAfterLoginAsync() => accounts.RememberCurrentAsync(remember.IsChecked);
}
#endif
