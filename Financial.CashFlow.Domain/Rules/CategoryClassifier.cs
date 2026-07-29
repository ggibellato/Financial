using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Rules;

public static class CategoryClassifier
{
    public static bool IsInvestment(this Category category) => category == Category.Investimento;
}
