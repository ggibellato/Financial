using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Financial.Integrations.GoogleCore;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;

namespace Financial.Integrations.GoogleCalendar;

internal sealed class GoogleCalendarOAuthClient : IGoogleCalendarOAuthClient
{
    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string RevokeEndpoint = "https://oauth2.googleapis.com/revoke";
    private const string UserInfoEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo";

    private readonly HttpClient _httpClient;

    public GoogleCalendarOAuthClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public string BuildAuthorizationUrl(string clientId, string redirectUri, string scope, string state)
    {
        var query = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = scope,
            ["state"] = state,
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["include_granted_scopes"] = "true"
        };

        var queryString = string.Join('&', query.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        return $"{AuthorizationEndpoint}?{queryString}";
    }

    public async Task<GoogleOAuthTokenResult> ExchangeCodeForTokenAsync(
        string clientId, string clientSecret, string redirectUri, string code, CancellationToken cancellationToken = default)
    {
        using var flow = CreateFlow(clientId, clientSecret);
        try
        {
            var tokenResponse = await flow.ExchangeCodeForTokenAsync(
                userId: "financial-user",
                code: code,
                redirectUri: redirectUri,
                taskCancellationToken: cancellationToken).ConfigureAwait(false);
            return ToResult(tokenResponse);
        }
        catch (TokenResponseException ex) when (IsInvalidGrant(ex))
        {
            throw new GoogleTokenRevokedException("Google rejected the authorization code.", ex);
        }
    }

    public async Task<GoogleOAuthTokenResult> RefreshAccessTokenAsync(
        string clientId, string clientSecret, string refreshToken, CancellationToken cancellationToken = default)
    {
        using var flow = CreateFlow(clientId, clientSecret);
        try
        {
            var tokenResponse = await flow.RefreshTokenAsync(
                userId: "financial-user",
                refreshToken: refreshToken,
                taskCancellationToken: cancellationToken).ConfigureAwait(false);
            return ToResult(tokenResponse);
        }
        catch (TokenResponseException ex) when (IsInvalidGrant(ex))
        {
            throw new GoogleTokenRevokedException("The stored refresh token was rejected by Google.", ex);
        }
    }

    public async Task RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = token });
        using var response = await _httpClient.PostAsync(RevokeEndpoint, content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> GetAccountEmailAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, UserInfoEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var userInfo = await response.Content.ReadFromJsonAsync<GoogleUserInfoResponse>(cancellationToken).ConfigureAwait(false);
        return userInfo?.Email ?? throw new InvalidOperationException("Google's userinfo response did not include an email address.");
    }

    public async Task<string> CreateCalendarAsync(string accessToken, string calendarName, CancellationToken cancellationToken = default)
    {
        using var service = CreateCalendarService(accessToken);
        var created = await GoogleRetryPolicy.ExecuteWithRetryAsync(
            () => service.Calendars.Insert(new Calendar { Summary = calendarName }).ExecuteAsync(cancellationToken)).ConfigureAwait(false);
        return created.Id;
    }

    public async Task DeleteCalendarAsync(string accessToken, string calendarId, CancellationToken cancellationToken = default)
    {
        using var service = CreateCalendarService(accessToken);
        await GoogleRetryPolicy.ExecuteWithRetryAsync(async () =>
        {
            await service.Calendars.Delete(calendarId).ExecuteAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }).ConfigureAwait(false);
    }

    private static GoogleAuthorizationCodeFlow CreateFlow(string clientId, string clientSecret) =>
        new(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
            Scopes = new[] { CalendarService.Scope.Calendar }
        });

    private static CalendarService CreateCalendarService(string accessToken) => new(new BaseClientService.Initializer
    {
        HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
        ApplicationName = "Financial"
    });

    private static GoogleOAuthTokenResult ToResult(TokenResponse tokenResponse) => new(
        tokenResponse.AccessToken,
        tokenResponse.RefreshToken,
        tokenResponse.IssuedUtc.AddSeconds(tokenResponse.ExpiresInSeconds ?? 0));

    private static bool IsInvalidGrant(TokenResponseException ex) =>
        string.Equals(ex.Error?.Error, "invalid_grant", StringComparison.OrdinalIgnoreCase);

    private sealed class GoogleUserInfoResponse
    {
        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }
}
