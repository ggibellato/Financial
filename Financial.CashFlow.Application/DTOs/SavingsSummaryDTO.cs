namespace Financial.CashFlow.Application.DTOs;

public sealed class SavingsSummaryDTO
{
    /// <summary>How often interest is paid: "Monthly" or "Daily".</summary>
    public required string InterestFrequency { get; init; }
    public int? PaymentDay { get; init; }
    public required bool IsTaxFree { get; init; }
    /// <summary>The latest rate in effect today; null when every rate is in the future.</summary>
    public InterestRateDTO? CurrentRate { get; init; }
    /// <summary>The next rate taking effect after today, if any.</summary>
    public InterestRateDTO? UpcomingRate { get; init; }
}
