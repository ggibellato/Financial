using System.Net.Sockets;
using Financial.Presentation.App.Services;
using FluentAssertions;

namespace Financial.Presentation.Tests.Services;

/// <summary>
/// Covers <see cref="CalendarOAuthCallbackListener"/>'s query-parsing/redirect-extraction logic
/// and cancellation behaviour as fast, deterministic unit tests. A real end-to-end run (a live
/// HttpListener bind, a real HTTP client hitting it, a real response written back) was tried and
/// deliberately removed: it was refused outright by local security software on at least one dev
/// machine, and in CI it silently zeroed out this entire test assembly's coverage collection
/// (all 1300+ tests still reported "Passed", but coverlet recorded zero hits everywhere) - a
/// data-collection-level failure a per-class coverage exclude cannot fix, since it isn't scoped
/// to the one class using the socket. The class is excluded from the coverage gate
/// (coverlet.runsettings) accordingly; its raw HttpListener bind/accept/write path is verified
/// manually instead - run Financial.App and complete a real Google Calendar connect (see F04's
/// spec.md addendum).
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

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
