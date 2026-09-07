namespace Financial.CashFlow.Application.DTOs;

public sealed class GoogleCalendarCallbackResultDTO
{
    public required bool Success { get; init; }

    public string? ErrorMessage { get; init; }
}
