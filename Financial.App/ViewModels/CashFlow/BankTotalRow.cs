namespace Financial.Presentation.App.ViewModels.CashFlow;

/// <summary>One row of the read-only Summary Banks grid: a bank's balance and round-up total, mirroring useMonthly.ts's BankTotal.</summary>
public sealed class BankTotalRow
{
    public required string Bank { get; init; }
    public required decimal Balance { get; init; }
    public required decimal RoundUpTotal { get; init; }
}
