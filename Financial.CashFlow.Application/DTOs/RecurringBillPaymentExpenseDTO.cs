namespace Financial.CashFlow.Application.DTOs;

public sealed class RecurringBillPaymentExpenseDTO
{
    public required DateOnly Date { get; init; }

    /// <summary>Free-text description, up to 200 characters.</summary>
    public required string Description { get; init; }

    public required decimal Value { get; init; }

    public required Guid CategoryId { get; init; }

    public required Guid PaymentSourceBankId { get; init; }
}
