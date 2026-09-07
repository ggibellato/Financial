namespace Financial.CashFlow.Application.Models;

/// <summary>
/// Google only re-issues <see cref="RefreshToken"/> on first consent (the app always requests
/// consent, so an exchange always carries one); a plain refresh returns a null
/// <see cref="RefreshToken"/>, meaning "keep the one already stored".
/// </summary>
public sealed record GoogleCalendarTokenResult(string AccessToken, string? RefreshToken, DateTimeOffset AccessTokenExpiresAtUtc);
