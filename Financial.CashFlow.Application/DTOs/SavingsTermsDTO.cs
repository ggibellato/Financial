namespace Financial.CashFlow.Application.DTOs;

public sealed class SavingsTermsDTO
{
    /// <summary>How often interest is paid: "Monthly" or "Daily".</summary>
    public required string InterestFrequency { get; init; }
    /// <summary>Day of the month interest is paid (1-31), only for "Monthly".</summary>
    public int? PaymentDay { get; init; }
    public required bool IsTaxFree { get; init; }
    public required IReadOnlyList<InterestRateDTO> Rates { get; init; }
    public bool RoundUpOnly { get; init; }
}
