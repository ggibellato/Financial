namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for a bank's running balance through the end of a given month.
/// </summary>
public sealed class BankBalanceDTO
{
    public required string Bank { get; init; }

    /// <summary>Opening balance plus income minus (round-up-adjusted) expenses, from the bank's opening date through the end of the requested month.</summary>
    public required decimal Balance { get; init; }
}
