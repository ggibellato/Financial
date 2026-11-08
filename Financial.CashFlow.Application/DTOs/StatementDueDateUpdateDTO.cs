namespace Financial.CashFlow.Application.DTOs;

public sealed class StatementDueDateUpdateDTO
{
    public required DateOnly? DueDate { get; init; }
}
