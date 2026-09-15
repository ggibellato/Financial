using Financial.Presentation.App.ViewModels.Investment;

namespace Financial.Presentation.App.Helpers;

public static class LotAllocationCalculator
{
    // Matches Financial.Web's lotAllocation.ts tolerance - an exact decimal sum can still miss
    // by a rounding-error fraction of a unit, which would otherwise block a genuinely exact sale.
    private const decimal QuantityTolerance = 0.000000001m;

    public static decimal SumAllocations(IEnumerable<LotAllocationRowViewModel> rows) =>
        rows.Sum(row => row.Quantity);

    public static bool IsAllocationExact(decimal allocated, decimal saleQuantity) =>
        saleQuantity > 0 && Math.Abs(allocated - saleQuantity) <= QuantityTolerance;

    public static bool IsLotOverAllocated(LotAllocationRowViewModel row) =>
        row.Quantity - row.RemainingQuantity > QuantityTolerance;

    public static bool HasOverAllocatedLot(IEnumerable<LotAllocationRowViewModel> rows) =>
        rows.Any(IsLotOverAllocated);
}
