namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to mark a card statement paid, naming the bank account that paid it.
/// </summary>
public sealed class MarkStatementPaidDTO
{
    /// <summary>Payment source name that settled the statement.</summary>
    public string? PaymentSource { get; init; }
}
