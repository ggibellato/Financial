namespace Financial.CashFlow.Application.DTOs;

public sealed class SettlePotWithdrawalsDTO
{
    public Guid? PaymentSourceBankId { get; init; }
    public DateOnly? Date { get; init; }
}
