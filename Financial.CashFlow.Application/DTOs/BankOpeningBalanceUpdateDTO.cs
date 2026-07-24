namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to update a bank's opening balance and effective date.
/// </summary>
public sealed class BankOpeningBalanceUpdateDTO
{
    /// <summary>Real-world balance as of <see cref="OpeningBalanceDate"/>.</summary>
    public required decimal OpeningBalance { get; init; }

    /// <summary>The date <see cref="OpeningBalance"/> is accurate as of.</summary>
    public required DateOnly OpeningBalanceDate { get; init; }
}
