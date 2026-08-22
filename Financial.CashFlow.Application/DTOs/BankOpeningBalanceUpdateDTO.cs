namespace Financial.CashFlow.Application.DTOs;

public sealed class BankOpeningBalanceUpdateDTO
{
    /// <summary>Real-world balance as of <see cref="OpeningBalanceDate"/>.</summary>
    public required decimal OpeningBalance { get; init; }

    /// <summary>The date <see cref="OpeningBalance"/> is accurate as of.</summary>
    public required DateOnly OpeningBalanceDate { get; init; }
}
