using System.Net.Http;
using System.Net.Sockets;
using Financial.Presentation.App.Services;
using FluentAssertions;

namespace Financial.Presentation.Tests.Services;

/// <summary>
/// Covers <see cref="CalendarOAuthCallbackListener"/>'s query-parsing/redirect-extraction logic
/// and cancellation behaviour as fast, deterministic unit tests (no real socket), plus one real
/// end-to-end run (real HttpListener bind, real HTTP client, real response written back) for
/// coverage of the actual I/O path. That last test was unreliable on at least one dev machine,
/// where every loopback connection attempt to the freshly-built test executable was refused for
/// far longer than any one-time local-security-software delay should take - if it hangs or fails
/// locally for you, verify manually instead (run Financial.App and complete a real Google
/// Calendar connect - see F04's spec.md addendum) rather than fighting the local environment; it
/// is expected to pass in CI, which has no such interference.
/// </summary>
public class CalendarOAuthCallbackListenerTests
{
    [Fact]
    public void ExtractRedirectUri_ParsesTheRedirectUriQueryParameter()
    {
        var url = "https://accounts.google.com/o/oauth2/v2/auth?client_id=x&redirect_uri=" +
                   Uri.EscapeDataString("http://localhost:8082/") + "&state=abc";

        var result = CalendarOAuthCallbackListener.ExtractRedirectUri(url);

        result.Should().Be(new Uri("http://localhost:8082/"));
    }

    [Fact]
    public void ExtractRedirectUri_MissingRedirectUri_Throws()
    {
        Action act = () => CalendarOAuthCallbackListener.ExtractRedirectUri("https://accounts.google.com/o/oauth2/v2/auth?client_id=x");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ExtractRedirectUri_BlankRedirectUri_Throws()
    {
        Action act = () => CalendarOAuthCallbackListener.ExtractRedirectUri("https://accounts.google.com/o/oauth2/v2/auth?redirect_uri=");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ParseQuery_NullOrEmpty_ReturnsEmptyDictionary()
    {
        CalendarOAuthCallbackListener.ParseQuery(null).Should().BeEmpty();
        CalendarOAuthCallbackListener.ParseQuery(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void ParseQuery_LeadingQuestionMark_IsStripped()
    {
        var result = CalendarOAuthCallbackListener.ParseQuery("?code=abc&state=xyz");

        result.Should().Contain("code", "abc").And.Contain("state", "xyz");
    }

    [Fact]
    public void ParseQuery_UrlEncodedValues_AreDecoded()
    {
        var result = CalendarOAuthCallbackListener.ParseQuery("?redirect_uri=http%3A%2F%2Flocalhost%3A8082%2F");

        result["redirect_uri"].Should().Be("http://localhost:8082/");
    }

    [Fact]
    public void ParseQuery_KeyWithoutValue_MapsToEmptyString()
    {
        var result = CalendarOAuthCallbackListener.ParseQuery("?error");

        result["error"].Should().BeEmpty();
    }

    [Fact]
    public async Task ListenAsync_WhenCancelledBeforeGoogleRedirects_ThrowsOperationCanceled()
    {
        var port = GetFreeTcpPort();
        var authorizationUrl = "https://accounts.google.com/o/oauth2/v2/auth" +
            $"?client_id=fake-client-id&state=xyz&redirect_uri={Uri.EscapeDataString($"http://localhost:{port}/")}";
        var sut = new CalendarOAuthCallbackListener();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        Func<Task> act = async () => await sut.ListenAsync(authorizationUrl, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ListenAsync_ReceivesGooglesRedirect_WritesTheLandingPage_AndExtractsCodeAndState()
    {
        var port = GetFreeTcpPort();
        var authorizationUrl = "https://accounts.google.com/o/oauth2/v2/auth" +
            $"?client_id=fake-client-id&state=xyz&redirect_uri={Uri.EscapeDataString($"http://localhost:{port}/")}";
        var sut = new CalendarOAuthCallbackListener();
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        var listenTask = sut.ListenAsync(authorizationUrl);
        var response = await GetWithRetryAsync(httpClient, $"http://localhost:{port}/?code=auth-code-123&state=xyz");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        var result = await listenTask;

        result.Code.Should().Be("auth-code-123");
        result.State.Should().Be("xyz");
        result.Error.Should().BeNull();
        body.Should().Contain("close this window");
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task<HttpResponseMessage> GetWithRetryAsync(HttpClient httpClient, string url, int timeoutMs = 30000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (true)
        {
            try
            {
                return await httpClient.GetAsync(url);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && DateTime.UtcNow < deadline)
            {
                await Task.Delay(50);
            }
        }
    }
}
