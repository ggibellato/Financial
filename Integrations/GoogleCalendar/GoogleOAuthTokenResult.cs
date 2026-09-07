namespace Financial.Integrations.GoogleCalendar;

/// <summary>
/// Google only re-issues <see cref="RefreshToken"/> on the first consent (or when the
/// authorization request used <c>prompt=consent</c>); a plain refresh call returns a new
/// <see cref="AccessToken"/> with a null <see cref="RefreshToken"/>, meaning "keep the one you have".
/// </summary>
public sealed record GoogleOAuthTokenResult(string AccessToken, string? RefreshToken, DateTimeOffset AccessTokenExpiresAtUtc);
