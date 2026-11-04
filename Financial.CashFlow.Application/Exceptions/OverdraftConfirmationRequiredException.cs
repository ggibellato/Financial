namespace Financial.CashFlow.Application.Exceptions;

public sealed class OverdraftConfirmationRequiredException : Exception
{
    public OverdraftConfirmationRequiredException(string potName, decimal balance)
        : base(FormattableString.Invariant($"This withdrawal exceeds {potName}'s balance of {balance:F2}. Set confirmed=true to proceed."))
    {
        PotName = potName;
        Balance = balance;
    }

    public string PotName { get; }
    public decimal Balance { get; }
}
