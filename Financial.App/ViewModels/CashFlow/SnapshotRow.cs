using Financial.CashFlow.Application.DTOs;

namespace Financial.Presentation.App.ViewModels.CashFlow;

public sealed class SnapshotRow
{
    public required Guid Id { get; init; }
    public required string Account { get; init; }
    public required bool IsLiability { get; init; }
    public required decimal Value { get; init; }

    public string DisplayLabel => IsLiability ? $"{Account} (liability)" : Account;

    public static SnapshotRow FromDto(InvestmentSnapshotDTO dto) => new()
    {
        Id = dto.Id,
        Account = dto.AccountName,
        IsLiability = dto.IsLiability,
        Value = dto.Value,
    };
}
