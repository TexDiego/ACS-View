using ACS_View.Application.Updates;
using System.Text.Json;

var checks = 0;
string Release(string tag = "v1.1.0", string body = "", bool prerelease = false,
    string name = "ACSView-v1.1.0.apk", string url = "https://github.com/TexDiego/ACS-View/releases/download/v1.1.0/ACSView-v1.1.0.apk") =>
    JsonSerializer.Serialize(new { tag_name = tag, body, draft = false, prerelease,
        assets = new[] { new { name, browser_download_url = url, size = 123, digest = "sha256:abc" } } });
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
}
var optional = "<!-- acs-update {\"versionCode\":2,\"minimumVersionCode\":1} -->";
var required = "<!-- acs-update {\"versionCode\":3,\"minimumVersionCode\":2} -->";
Check(GitHubReleaseUpdate.Parse(Release(), "1.0.0", 1) is { Required: false }, "Legacy releases are optional");
Check(GitHubReleaseUpdate.Parse(Release(body: optional), "1.0.0", 1) is { Required: false, VersionCode: 2 }, "Optional metadata");
Check(GitHubReleaseUpdate.Parse(Release(body: required), "1.0.0", 1) is { Required: true }, "Below minimum is mandatory");
Check(GitHubReleaseUpdate.Parse(Release(body: required), "1.0.0", 2) is { Required: false }, "Minimum supported remains optional");
Check(GitHubReleaseUpdate.Parse(Release(body: optional), "1.1.0", 2) is null, "Same build");
Check(GitHubReleaseUpdate.Parse(Release(body: optional), "1.1.0", 1) is null, "Installed display version suppresses inconsistent higher feed code");
Check(GitHubReleaseUpdate.Parse(Release(body: required), "1.1.0", 0) is null, "Unreadable build does not require reinstalling the installed release");
Check(GitHubReleaseUpdate.Parse(Release(body: required), "1.2.0", 1) is null, "Higher feed code cannot downgrade the display version");
Check(GitHubReleaseUpdate.Parse(Release(), "1.1", 0) is null, "Missing zero version components are equivalent");
Check(GitHubReleaseUpdate.Parse(Release(), "1.1.0.0", 0) is null, "Zero revision is equivalent");
Check(GitHubReleaseUpdate.Parse(Release(body: optional), "1.0.0", 2) is null, "Newer display version cannot downgrade the build code");
Check(GitHubReleaseUpdate.Parse(Release(body: optional), "unknown", 1) is null, "Unknown installed version does not advertise an unverified update");
Check(GitHubReleaseUpdate.Parse(Release(body: optional), "1.0.0", 1) is { Version: "1.1.0" }, "Display release version stays readable");
Check(GitHubReleaseUpdate.Parse(Release(body: optional), "1.2.0", 4) is null, "No downgrade");
Check(GitHubReleaseUpdate.Parse(Release(), "1.1.0", 2) is null, "Same legacy version");
Check(GitHubReleaseUpdate.Parse(Release(prerelease: true), "1.0.0", 1) is null, "Ignore prerelease");
Check(GitHubReleaseUpdate.Parse(Release(name: "other.apk"), "1.0.0", 1) is null, "Correct APK only");
Check(GitHubReleaseUpdate.Parse(Release(url: "https://evil.example/update.apk"), "1.0.0", 1) is null, "Trusted repository only");
Check(GitHubReleaseUpdate.Parse(Release(body: "<!-- acs-update {}"), "1.0.0", 1) is null, "Malformed marker");
Check(GitHubReleaseUpdate.Parse(Release(body: "<!-- acs-update {\"versionCode\":2,\"minimumVersionCode\":3} -->"), "1.0.0", 1) is null, "Invalid minimum");
using (var handler = new FeedHandler((_, _) => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(Release(body: required)) })))
using (var client = new HttpClient(handler))
{
    var checker = new GitHubUpdateChecker(client);
    Check(await checker.CheckAsync(false, "1.0.0", 1) is null && handler.Calls == 0, "Offline makes zero requests");
    Check(await checker.CheckAsync(true, "1.0.0", 1) is { Required: true } && handler.Calls == 1, "Online feed finds required update");
}
foreach (var status in new[] { System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.Forbidden })
{
    using var client = new HttpClient(new FeedHandler((_, _) => Task.FromResult(new HttpResponseMessage(status))));
    Check(await new GitHubUpdateChecker(client).CheckAsync(true, "1.0.0", 1) is null, $"Fail open on {status}");
}
using (var client = new HttpClient(new FeedHandler((_, _) => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("invalid json") }))))
    Check(await new GitHubUpdateChecker(client).CheckAsync(true, "1.0.0", 1) is null, "Malformed feed does not block login");
using (var client = new HttpClient(new FeedHandler(async (_, token) =>
{
    await Task.Delay(TimeSpan.FromSeconds(30), token);
    return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
})))
{
    var timer = System.Diagnostics.Stopwatch.StartNew();
    Check(await new GitHubUpdateChecker(client).CheckAsync(true, "1.0.0", 1) is null && timer.Elapsed < TimeSpan.FromSeconds(5), "Slow feed is cancelled within short deadline");
}
Console.WriteLine($"Passed {checks} update checks.");

sealed class FeedHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    public int Calls { get; private set; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        return send(request, cancellationToken);
    }
}
