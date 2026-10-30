namespace Financial.CashFlow.Application.DTOs;

public sealed class AccountTemplateGroupDTO
{
    public required string TemplateName { get; init; }
    public required IReadOnlyList<AccountGroupMemberDTO> Members { get; init; }
    public required IReadOnlyList<AccountTemplateGroupTotalDTO> Totals { get; init; }
}
