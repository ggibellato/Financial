namespace Financial.CashFlow.Application.DTOs;

public sealed class AccountTemplateDTO
{
    public required string Name { get; init; }
    public required IReadOnlyList<string> Currencies { get; init; }
    public required IReadOnlyList<string> Capabilities { get; init; }
}
