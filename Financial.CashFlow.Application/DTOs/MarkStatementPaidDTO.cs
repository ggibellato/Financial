namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to mark a card statement paid, identifying the bank account that paid it.
/// </summary>
public sealed class MarkStatementPaidDTO
{
    /// <summary>Identifier of the bank account that settled the statement.</summary>
    public Guid? PaymentSourceBankId { get; init; }
}
