using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Domain.Entities;

namespace Financial.Presentation.App.ViewModels.CashFlow;

public static class WithdrawalCategoryRules
{
    public static IReadOnlyList<CategoryDTO> Eligible(IEnumerable<CategoryDTO> categories) =>
        categories.Where(c => c.Active && !c.IsInvestment && !SameName(c.Name, Category.ReservaName)).ToList();

    public static Guid? DefaultFor(IEnumerable<CategoryDTO> eligibleCategories, string? bucketName) =>
        eligibleCategories.FirstOrDefault(c => SameName(c.Name, bucketName))?.Id;

    private static bool SameName(string? left, string? right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
