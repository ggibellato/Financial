namespace Financial.Investment.Application.DTOs;

public class FxRateSnapshotDTO
{
    public required string ToCurrency { get; set; }
    public decimal Rate { get; set; }
    public required string Source { get; set; }
    public DateTimeOffset RetrievedAt { get; set; }
}
