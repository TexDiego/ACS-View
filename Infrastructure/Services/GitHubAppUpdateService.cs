using ACS_View.Application.Interfaces;
using ACS_View.Application.Updates;
using System.Security.Cryptography;

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

    public async Task InstallAsync(AppUpdate update, CancellationToken cancellationToken)
    {
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
            using var response = await Client.GetAsync(update.ApkUrl, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            response.EnsureSuccessStatusCode();
            await using (var source = await response.Content.ReadAsStreamAsync(timeout.Token))
            await using (var target = File.Create(path))
                await source.CopyToAsync(target, timeout.Token);
            if (new FileInfo(path).Length != update.Size) throw new InvalidOperationException("O download ficou incompleto. Tente novamente.");
            if (update.Digest?.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) == true)
            {
                await using var file = File.OpenRead(path);
                var hash = Convert.ToHexString(await SHA256.HashDataAsync(file, timeout.Token));
                if (!hash.Equals(update.Digest[7..], StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Não foi possível verificar o APK. Tente novamente.");
            }
            if (!await Launcher.Default.OpenAsync(new OpenFileRequest("Instalar atualização", new ReadOnlyFile(path, "application/vnd.android.package-archive"))))
                throw new InvalidOperationException("O Android não conseguiu abrir o instalador.");
        }
        catch
        {
            if (File.Exists(path)) File.Delete(path);
            throw;
        }
    }
}
