namespace Financial.CashFlow.Application.DTOs;

public sealed class BankOpeningBalanceUpdateDTO
{
    /// <summary>Real-world balance as of <see cref="OpeningBalanceDate"/>.</summary>
    public required decimal OpeningBalance { get; init; }

    public required DateOnly OpeningBalanceDate { get; init; }
}
