namespace Financial.CashFlow.Application.DTOs;

public sealed class CalendarCallbackResultDTO
{
    public required bool Success { get; init; }

    public string? ErrorMessage { get; init; }
}
