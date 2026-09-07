namespace Financial.CashFlow.Infrastructure.Configuration;

/// <summary>
/// Google-specific OAuth client settings, consumed only by <c>GoogleCalendarProviderAdapter</c>.
/// Lives in Infrastructure, not Application, because these are the first (and current) calendar
/// provider's own concrete settings - a second provider would bring its own settings type here,
/// not extend this one.
/// </summary>
public sealed class GoogleCalendarSettingsOptions
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? RedirectUri { get; set; }
    public string? CredentialsPath { get; set; }
}
