namespace Financial.CashFlow.Application.Configuration;

public sealed class GoogleCalendarSettingsOptions
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? RedirectUri { get; set; }
    public string? CredentialsPath { get; set; }
}
