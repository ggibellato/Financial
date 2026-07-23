namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to edit a single Reserva movement's fields.
/// </summary>
public sealed class UpdateReserveMovementDTO
{
    public required string Bucket { get; init; }
    public required decimal Amount { get; init; }
    public required DateOnly Date { get; init; }
    public required string Description { get; init; }
}
