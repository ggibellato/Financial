using Financial.CashFlow.Infrastructure.Persistence;

namespace Financial.CashFlow.Infrastructure.Tests;

internal static class TestCashFlowSeed
{
    public static string AsWrittenBy(ICashFlowSerializer serializer) =>
        serializer.Serialize(serializer.Deserialize(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "SeedData", "data-cashflow.test.json"))));
}
