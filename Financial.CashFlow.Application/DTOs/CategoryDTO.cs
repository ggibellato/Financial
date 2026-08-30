namespace Financial.CashFlow.Application.DTOs;

public sealed class CategoryDTO
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    /// <summary>Whether this category currently accepts new expenses.</summary>
    public required bool Active { get; init; }

    public required bool IsInvestment { get; init; }

    public required bool IsTithe { get; init; }

    /// <summary>Whether an expense still references this category - Delete is refused (409) while
    /// this is true.</summary>
    public required bool HasReferences { get; init; }
}
