using System.Text.Json;

namespace ACS_View.Application.Updates;

public sealed record AppUpdate(string Version, long VersionCode, bool Required, Uri ApkUrl, long Size, string? Digest);

public static class GitHubReleaseUpdate
{
    public const string Repository = "TexDiego/ACS-View";
    public const string Marker = "<!-- acs-update ";

    public static AppUpdate? Parse(string json, string installedVersion, long installedCode)
    {
        using var document = JsonDocument.Parse(json);
        var release = document.RootElement;
        if (release.GetProperty("draft").GetBoolean() || release.GetProperty("prerelease").GetBoolean()) return null;
        var tag = release.GetProperty("tag_name").GetString() ?? "";
        if (!Version.TryParse(tag.TrimStart('v'), out var version)) return null;
        long code = 0, minimum = 0;
        var body = release.TryGetProperty("body", out var notes) ? notes.GetString() ?? "" : "";
        var start = body.IndexOf(Marker, StringComparison.Ordinal);
        if (start >= 0)
        {
            start += Marker.Length;
            var end = body.IndexOf(" -->", start, StringComparison.Ordinal);
            if (end < 0) return null;
            using var metadata = JsonDocument.Parse(body[start..end]);
            code = metadata.RootElement.GetProperty("versionCode").GetInt64();
            minimum = metadata.RootElement.GetProperty("minimumVersionCode").GetInt64();
            if (code <= 0 || minimum < 0 || minimum > code) return null;
        }
        if (code > 0 ? code <= installedCode : !Version.TryParse(installedVersion, out var current) || version <= current)
            return null;
        foreach (var asset in release.GetProperty("assets").EnumerateArray())
        {
            if (asset.GetProperty("name").GetString() != $"ACSView-{tag}.apk") continue;
            if (!Uri.TryCreate(asset.GetProperty("browser_download_url").GetString(), UriKind.Absolute, out var url)
                || url.Scheme != "https" || url.Host != "github.com"
                || !url.AbsolutePath.StartsWith($"/{Repository}/releases/download/", StringComparison.Ordinal)) continue;
            var size = asset.GetProperty("size").GetInt64();
            if (size <= 0) continue;
            return new(version.ToString(), code, installedCode < minimum, url, size,
                asset.TryGetProperty("digest", out var digest) ? digest.GetString() : null);
        }
        return null;
    }
}
