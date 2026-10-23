using ACS_View.Application.Interfaces;
using System.Security.Cryptography;

namespace ACS_View.Application.Updates;

public static class AppUpdateDownloader
{
    public static async Task DownloadAsync(HttpClient client, AppUpdate update, string path,
        IProgress<AppUpdateProgress>? progress, CancellationToken cancellationToken)
    {
        try
        {
            progress?.Report(new(0, "Conectando ao servidor de atualização…"));
            using var response = await client.GetAsync(update.ApkUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var target = File.Create(path))
            {
                var buffer = new byte[81920];
                long downloaded = 0;
                var lastReport = DateTime.MinValue;
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    downloaded += read;
                    if (DateTime.UtcNow - lastReport < TimeSpan.FromMilliseconds(150)) continue;
                    lastReport = DateTime.UtcNow;
                    var fraction = update.Size > 0 ? Math.Clamp((double)downloaded / update.Size, 0, 1) : 0;
                    progress?.Report(new(fraction * .9, $"Baixando atualização · {fraction:P0} ({downloaded / 1048576d:F1} de {update.Size / 1048576d:F1} MB)"));
                }
            }
            progress?.Report(new(.92, "Verificando o arquivo baixado…"));
            if (new FileInfo(path).Length != update.Size) throw new InvalidOperationException("O download ficou incompleto. Tente novamente.");
            if (update.Digest?.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) == true)
            {
                await using var file = File.OpenRead(path);
                var hash = Convert.ToHexString(await SHA256.HashDataAsync(file, cancellationToken));
                if (!hash.Equals(update.Digest[7..], StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Não foi possível verificar o APK. Tente novamente.");
            }
        }
        catch
        {
            if (File.Exists(path)) File.Delete(path);
            throw;
        }
    }
}
