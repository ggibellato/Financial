namespace Financial.CashFlow.Application.DTOs;

public sealed class WithdrawalRequestDTO
{
    public required Guid BucketId { get; init; }

    /// <summary>Withdrawal amount, as a positive magnitude.</summary>
    public required decimal Amount { get; init; }

    public required DateOnly Date { get; init; }

    public required string Description { get; init; }

    /// <summary>Set true to proceed despite an overdraft warning.</summary>
    public bool Confirmed { get; init; }

    /// <summary>Bank the withdrawal passes through. Omit for a direct withdrawal; requires <see cref="ExpenseCategoryId"/> when set.</summary>
    public Guid? PaymentSourceBankId { get; init; }

    /// <summary>Category of the expense recorded on the bank. Required when <see cref="PaymentSourceBankId"/> is set, and rejected without it.</summary>
    public Guid? ExpenseCategoryId { get; init; }
}
