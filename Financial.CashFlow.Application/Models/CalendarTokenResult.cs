namespace Financial.CashFlow.Application.Models;

/// <summary>
/// An OAuth-style token exchange/refresh result. A provider only re-issues
/// <see cref="RefreshToken"/> on first consent; a plain refresh returns a null
/// <see cref="RefreshToken"/>, meaning "keep the one already stored".
/// </summary>
public sealed record CalendarTokenResult(string AccessToken, string? RefreshToken, DateTimeOffset AccessTokenExpiresAtUtc);
