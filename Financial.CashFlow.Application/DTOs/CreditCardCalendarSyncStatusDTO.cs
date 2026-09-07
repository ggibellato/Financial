namespace Financial.CashFlow.Application.DTOs;

public sealed class CreditCardCalendarSyncStatusDTO
{
    public required Guid CreditCardId { get; init; }

    public required string State { get; init; }

    public DateTimeOffset? LastSuccessfulSyncUtc { get; init; }

    public string? LastError { get; init; }
}
