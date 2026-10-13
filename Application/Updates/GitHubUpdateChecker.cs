namespace ACS_View.Application.Updates;

public sealed class GitHubUpdateChecker(HttpClient client)
{
    public async Task<AppUpdate?> CheckAsync(bool hasInternet, string version, long code)
    {
        if (!hasInternet) return null;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.github.com/repos/{GitHubReleaseUpdate.Repository}/releases/latest");
            request.Headers.Accept.ParseAdd("application/vnd.github+json");
            using var response = await client.SendAsync(request, timeout.Token);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(timeout.Token);
            return GitHubReleaseUpdate.Parse(json, version, code);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or System.Text.Json.JsonException
            or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        {
            // Authentication is local: an unavailable release feed must not prevent access.
            return null;
        }
    }
}
