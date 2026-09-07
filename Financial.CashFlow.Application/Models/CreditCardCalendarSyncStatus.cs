namespace Financial.CashFlow.Application.Models;

/// <summary>A credit card's current calendar-event sync status, tracked in memory only - this is
/// operational/observability state, not business data, so it does not need to survive a
/// restart.</summary>
public sealed record CreditCardCalendarSyncStatus(
    CreditCardCalendarSyncState State,
    DateTimeOffset? LastSuccessfulSyncUtc,
    string? LastError);
