using ACS_View.Application.Interfaces;
using ACS_View.Application.State;
using System.Reflection;

namespace ACS_View.Views
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        private readonly IAppStartupService appStartupService;
        private readonly TransientInputLifecycle inputLifecycle = new();
        public IServiceProvider ServiceProvider { get; private set; }
        public static string InstalledVersionLabel { get; } = $"Versão {DisplayVersion()}";

        private static string DisplayVersion() => typeof(App).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "ApplicationDisplayVersion")?.Value
            ?? AppInfo.Current.VersionString;

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            SQLitePCL.Batteries_V2.Init();
            ServiceProvider = serviceProvider;
            appStartupService = serviceProvider.GetRequiredService<IAppStartupService>();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = CreateShell(isAuthenticated: false);
            var window = new Window(shell);
#if WINDOWS
            // Keep desktop forms and popup action bars usable at the smallest size.
            window.MinimumWidth = 960;
            window.MinimumHeight = 640;
            window.Title = "ACS View";
            var titleBar = new TitleBar { Title = "ACS View", ForegroundColor = Colors.White };
            titleBar.SetAppThemeColor(VisualElement.BackgroundColorProperty,
                (Color)Resources["Primary"], (Color)Resources["DarkPrimaryLight"]);
            window.TitleBar = titleBar;
#endif
            // Backgrounding and minimizing preserve the authenticated Shell and
            // unfinished inputs. A new window/process follows the startup login flow.
            window.Resumed += (_, _) => _ = ReconcileRemindersAsync();
            _ = PrepareStartupLoginAsync(window);

            return window;
        }

        public IDisposable BeginExternalInteraction() => inputLifecycle.BeginExternalInteraction();

        public async Task ResetToAuthenticatedShellAsync()
        {
            await appStartupService.InitializeAsync();
            await ReconcileRemindersAsync();
            await ResetShellAsync("//overallview");
        }

        public Task ResetToLoginShellAsync()
        {
            return ResetShellAsync("//login");
        }

        private AppShell CreateShell(bool isAuthenticated)
        {
            return new AppShell(ServiceProvider, isAuthenticated);
        }

        private Task ResetShellAsync(string route)
        {
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var shell = CreateShell(route != "//login");
                var window = Windows.FirstOrDefault();

                if (window is null)
                {
                    return;
                }

                window.Page = shell;
                if (route == "//login")
                {
                    return;
                }

                await shell.GoToAsync(route);
            });
        }

        private async Task PrepareStartupLoginAsync(Window window)
        {
            var initialPage = window.Page;
            try
            {
                await appStartupService.InitializeAsync();
                await ReconcileRemindersAsync();

                // A closed application requires a new login. Only background/resume
                // keeps the existing authenticated Shell in memory.
                if (ReferenceEquals(window.Page, initialPage)
                    && initialPage is AppShell { LoginPage: { } loginPage })
                {
                    await loginPage.PrepareForLoginAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Nao foi possivel preparar login inicial: {ex.Message}");
            }
        }

        private async Task ReconcileRemindersAsync()
        {
            try
            {
                await appStartupService.InitializeAsync();
                await ServiceProvider.GetRequiredService<INoteReminderService>().ReconcileAsync();
            }
            catch (Exception)
            {
                // No note text or user data in diagnostics. Notes page retries visibly.
                System.Diagnostics.Debug.WriteLine("Não foi possível reconciliar os lembretes de notas.");
            }
        }
    }
}
