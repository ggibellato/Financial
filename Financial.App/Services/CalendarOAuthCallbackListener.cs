using System.Net;
using System.Text;

namespace Financial.Presentation.App.Services;

public sealed class CalendarOAuthCallbackListener : ICalendarOAuthCallbackListener
{
    private const string LandingPageHtml =
        "<html><body style=\"font-family:sans-serif;padding:32px\">" +
        "You can close this window and return to Financial.</body></html>";

    public async Task<CalendarOAuthCallbackResult> ListenAsync(string authorizationUrl, CancellationToken cancellationToken = default)
    {
        var redirectUri = ExtractRedirectUri(authorizationUrl);

        using var listener = new HttpListener();
        listener.Prefixes.Add($"{redirectUri.Scheme}://{redirectUri.Authority}/");
        listener.Start();

        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                listener.Stop();
            }
            catch (ObjectDisposedException)
            {
            }
        });

        HttpListenerContext context;
        try
        {
            context = await listener.GetContextAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException)
        {
            throw new OperationCanceledException(
                "The OAuth callback listener was stopped before Google redirected back.", ex, cancellationToken);
        }

        var query = ParseQuery(context.Request.Url?.Query);
        query.TryGetValue("code", out var code);
        query.TryGetValue("state", out var state);
        query.TryGetValue("error", out var error);

        await WriteLandingPageAsync(context.Response).ConfigureAwait(false);

        return new CalendarOAuthCallbackResult(code, state, error);
    }

    /// <summary>Internal (not private) so <c>Financial.Presentation.Tests</c> can pin the
    /// parsing behaviour directly, without needing a real HttpListener socket.</summary>
    internal static Uri ExtractRedirectUri(string authorizationUrl)
    {
        var query = ParseQuery(new Uri(authorizationUrl).Query);
        if (!query.TryGetValue("redirect_uri", out var redirectUri) || string.IsNullOrWhiteSpace(redirectUri))
        {
            throw new InvalidOperationException("The authorization URL did not include a redirect_uri.");
        }

        return new Uri(redirectUri);
    }

    private static async Task WriteLandingPageAsync(HttpListenerResponse response)
    {
        var buffer = Encoding.UTF8.GetBytes(LandingPageHtml);
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = buffer.Length;
        response.KeepAlive = false;
        await response.OutputStream.WriteAsync(buffer).ConfigureAwait(false);
        response.Close();
    }

    /// <summary>Internal (not private) so <c>Financial.Presentation.Tests</c> can pin the
    /// parsing behaviour directly, without needing a real HttpListener socket.</summary>
    internal static Dictionary<string, string> ParseQuery(string? query)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(query))
        {
            return result;
        }

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            result[key] = value;
        }

        return result;
    }
}
