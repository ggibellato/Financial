namespace Financial.Presentation.App.ViewModels.CashFlow;

public sealed record WithdrawalBankOption(Guid? Id, string Name)
{
    public static WithdrawalBankOption Direct { get; } = new(null, "No bank (direct)");

    public override string ToString() => Name;
}
