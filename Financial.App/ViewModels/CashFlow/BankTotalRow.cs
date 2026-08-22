namespace Financial.Presentation.App.ViewModels.CashFlow;

public sealed class BankTotalRow
{
    public required Guid BankId { get; init; }
    public required string Bank { get; init; }
    public required decimal Balance { get; init; }
    public required decimal RoundUpTotal { get; init; }
}
