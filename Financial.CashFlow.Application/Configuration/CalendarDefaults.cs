namespace Financial.CashFlow.Application.Configuration;

/// <summary>Shared constants for the calendar integration, used by both
/// <c>CalendarIntegrationService</c> (creates the calendar) and
/// <c>CreditCardCalendarSyncService</c> (recreates it during self-healing).</summary>
public static class CalendarDefaults
{
    public const string DedicatedCalendarName = "Financial - Credit Card Due Dates";
}
