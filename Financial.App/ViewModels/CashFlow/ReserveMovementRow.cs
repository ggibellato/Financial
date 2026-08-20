using Financial.CashFlow.Application.DTOs;

namespace Financial.Presentation.App.ViewModels.CashFlow;

/// <summary>
/// A Movements grid row. <see cref="GroupTotal"/> is set only on the last row of a same
/// Date+Description group (2+ movements) — how a split's total is shown when browsing
/// history. <see cref="IsPartOfGroup"/> is set on every row of such a group (used to pick
/// the delete-confirmation wording, since removing any one line of a split removes all of
/// them). Mirrors Financial.Web's useReserva.ts buildMovementRows.
/// </summary>
public sealed class ReserveMovementRow
{
    public required Guid Id { get; init; }
    public required Guid BucketId { get; init; }
    public required string BucketName { get; init; }
    public required decimal Amount { get; init; }
    public required DateOnly Date { get; init; }
    public required string Description { get; init; }
    public decimal? GroupTotal { get; init; }
    public bool IsPartOfGroup { get; init; }

    /// <summary>Bindable in a DataTrigger without the nullable-value-type comparison pitfall of binding directly to GroupTotal.</summary>
    public bool HasGroupTotal => GroupTotal.HasValue;

    public static List<ReserveMovementRow> BuildRows(IReadOnlyList<ReserveMovementDTO> movements)
    {
        var groups = movements
            .Select((movement, index) => (movement, index))
            .GroupBy(x => (x.movement.Date, x.movement.Description))
            .ToDictionary(
                g => g.Key,
                g => (Total: g.Sum(x => x.movement.Amount), Count: g.Count(), LastIndex: g.Max(x => x.index)));

        return movements
            .Select((movement, index) =>
            {
                var group = groups[(movement.Date, movement.Description)];
                return new ReserveMovementRow
                {
                    Id = movement.Id,
                    BucketId = movement.BucketId,
                    BucketName = movement.BucketName,
                    Amount = movement.Amount,
                    Date = movement.Date,
                    Description = movement.Description,
                    GroupTotal = group.Count > 1 && group.LastIndex == index ? group.Total : null,
                    IsPartOfGroup = group.Count > 1,
                };
            })
            .ToList();
    }
}
