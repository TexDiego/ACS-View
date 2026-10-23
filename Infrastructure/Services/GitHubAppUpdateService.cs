using ACS_View.Application.Interfaces;
using ACS_View.Application.Updates;

namespace ACS_View.Infrastructure.Services;

public sealed class GitHubAppUpdateService : IAppUpdateService
{
    private static readonly HttpClient Client = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ACSView-Android-Updater/1.0");
        return client;
    }

    public Task<AppUpdate?> CheckAsync()
    {
        return new GitHubUpdateChecker(Client).CheckAsync(
            DeviceInfo.Platform == DevicePlatform.Android && Connectivity.Current.NetworkAccess == NetworkAccess.Internet,
            AppInfo.Current.VersionString, long.TryParse(AppInfo.Current.BuildString, out var code) ? code : 0);
    }

    public async Task InstallAsync(AppUpdate update, CancellationToken cancellationToken, IProgress<AppUpdateProgress>? progress = null)
    {
        progress?.Report(new(0, "Preparando atualização…"));
#if ANDROID
        var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("Não foi possível abrir o instalador.");
        if (OperatingSystem.IsAndroidVersionAtLeast(26) && activity.PackageManager?.CanRequestPackageInstalls() != true)
        {
            var intent = new global::Android.Content.Intent(global::Android.Provider.Settings.ActionManageUnknownAppSources,
                global::Android.Net.Uri.Parse($"package:{activity.PackageName}"));
            activity.StartActivity(intent);
            throw new InvalidOperationException("Permita instalar atualizações do ACS View na tela do Android e, ao voltar, toque em Instalar novamente.");
        }
#endif
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            throw new InvalidOperationException("Conecte-se à internet para baixar a atualização.");
        var directory = Path.Combine(FileSystem.CacheDirectory, "sharing-root", "updates");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "ACSView-update.apk");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));
        try
        {
            await AppUpdateDownloader.DownloadAsync(Client, update, path, progress, timeout.Token);
            timeout.Token.ThrowIfCancellationRequested();
            progress?.Report(new(.98, "Abrindo o instalador do Android…"));
            if (!await Launcher.Default.OpenAsync(new OpenFileRequest("Instalar atualização", new ReadOnlyFile(path, "application/vnd.android.package-archive"))))
                throw new InvalidOperationException("O Android não conseguiu abrir o instalador.");
            progress?.Report(new(1, "Arquivo pronto. Confirme a instalação na tela do Android."));
        }
        catch
        {
            if (File.Exists(path)) File.Delete(path);
            throw;
        }
    }
}
