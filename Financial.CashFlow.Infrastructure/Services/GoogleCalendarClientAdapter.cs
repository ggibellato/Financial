using Financial.CashFlow.Application.Configuration;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Models;
using Financial.Integrations.GoogleCalendar;
using Microsoft.Extensions.Options;

namespace Financial.CashFlow.Infrastructure.Services;

/// <summary>
/// Translates Application's <see cref="IGoogleCalendarClient"/> into calls against
/// <c>Integrations/GoogleCalendar</c>'s <see cref="IGoogleCalendarOAuthClient"/>, injecting the
/// configured client id/secret/redirect URI and the fixed Calendar scope, and mapping the
/// Integrations-level <see cref="GoogleTokenRevokedException"/> into the Application-level
/// <see cref="GoogleCalendarTokenRevokedException"/> so Application never references the vendor
/// SDK's exception type.
/// </summary>
public sealed class GoogleCalendarClientAdapter : IGoogleCalendarClient
{
    private const string CalendarScope = "https://www.googleapis.com/auth/calendar";

    private readonly IGoogleCalendarOAuthClient _oAuthClient;
    private readonly GoogleCalendarSettingsOptions _settings;

    public GoogleCalendarClientAdapter(IGoogleCalendarOAuthClient oAuthClient, IOptions<GoogleCalendarSettingsOptions> settings)
    {
        _oAuthClient = oAuthClient ?? throw new ArgumentNullException(nameof(oAuthClient));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    public string BuildAuthorizationUrl(string state) =>
        _oAuthClient.BuildAuthorizationUrl(RequireClientId(), RequireRedirectUri(), CalendarScope, state);

    public async Task<GoogleCalendarTokenResult> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default)
    {
        var result = await TranslatingRevocation(() =>
            _oAuthClient.ExchangeCodeForTokenAsync(RequireClientId(), RequireClientSecret(), RequireRedirectUri(), code, cancellationToken))
            .ConfigureAwait(false);
        return ToResult(result);
    }

    public async Task<GoogleCalendarTokenResult> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var result = await TranslatingRevocation(() =>
            _oAuthClient.RefreshAccessTokenAsync(RequireClientId(), RequireClientSecret(), refreshToken, cancellationToken))
            .ConfigureAwait(false);
        return ToResult(result);
    }

    public Task RevokeTokenAsync(string token, CancellationToken cancellationToken = default) =>
        _oAuthClient.RevokeTokenAsync(token, cancellationToken);

    public Task<string> GetAccountEmailAsync(string accessToken, CancellationToken cancellationToken = default) =>
        _oAuthClient.GetAccountEmailAsync(accessToken, cancellationToken);

    public Task<string> CreateCalendarAsync(string accessToken, string calendarName, CancellationToken cancellationToken = default) =>
        _oAuthClient.CreateCalendarAsync(accessToken, calendarName, cancellationToken);

    public Task DeleteCalendarAsync(string accessToken, string calendarId, CancellationToken cancellationToken = default) =>
        _oAuthClient.DeleteCalendarAsync(accessToken, calendarId, cancellationToken);

    private static async Task<GoogleOAuthTokenResult> TranslatingRevocation(Func<Task<GoogleOAuthTokenResult>> action)
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (GoogleTokenRevokedException ex)
        {
            throw new GoogleCalendarTokenRevokedException(ex.Message);
        }
    }

    private static GoogleCalendarTokenResult ToResult(GoogleOAuthTokenResult result) =>
        new(result.AccessToken, result.RefreshToken, result.AccessTokenExpiresAtUtc);

    private string RequireClientId() =>
        _settings.ClientId ?? throw MissingConfig(nameof(GoogleCalendarSettingsOptions.ClientId));

    private string RequireClientSecret() =>
        _settings.ClientSecret ?? throw MissingConfig(nameof(GoogleCalendarSettingsOptions.ClientSecret));

    private string RequireRedirectUri() =>
        _settings.RedirectUri ?? throw MissingConfig(nameof(GoogleCalendarSettingsOptions.RedirectUri));

    private static InvalidOperationException MissingConfig(string settingName) =>
        new($"CashFlow:GoogleCalendar:{settingName} is not configured.");
}
