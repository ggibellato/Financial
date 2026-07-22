using Financial.CashFlow.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using FluentAssertions;

namespace Financial.CashFlow.Infrastructure.Tests.Persistence;

public class CashFlowLoaderTests
{
    [Fact]
    public void LoadSync_WhenFileDoesNotExist_ReturnsEmptyCashFlowData()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"cashflow-missing-{Guid.NewGuid()}.json");
        var storage = new LocalJsonStorage(missingPath);
        var serializer = new CashFlowSerializerAdapter();

        var data = CashFlowLoader.LoadSync(storage, serializer);

        data.Expenses.Should().BeEmpty();
        data.ReserveMovements.Should().BeEmpty();
        data.CardStatements.Should().BeEmpty();
        data.RecurringBillTemplates.Should().BeEmpty();
        data.RecurringBillInstances.Should().BeEmpty();
        data.MaeLedgerEntries.Should().BeEmpty();
        data.InvestmentSnapshots.Should().BeEmpty();
    }

    [Fact]
    public void LoadSync_WhenFileIsMalformed_PropagatesParseException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cashflow-malformed-{Guid.NewGuid()}.json");
        File.WriteAllText(path, "{ not valid json");
        var storage = new LocalJsonStorage(path);
        var serializer = new CashFlowSerializerAdapter();

        try
        {
            var act = () => CashFlowLoader.LoadSync(storage, serializer);

            act.Should().Throw<Exception>();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadSync_WhenFileIsValid_DeserializesExistingData()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cashflow-valid-{Guid.NewGuid()}.json");
        var serializer = new CashFlowSerializerAdapter();
        var original = Financial.CashFlow.Domain.Entities.CashFlowData.Create();
        original.AddExpense(Financial.CashFlow.Domain.Entities.Expense.Create(
            new DateOnly(2026, 7, 1),
            "Test expense",
            10m,
            Financial.CashFlow.Domain.Enums.Category.Casa,
            Financial.CashFlow.Domain.Enums.PaymentSource.Chase,
            null));
        File.WriteAllText(path, serializer.Serialize(original));
        var storage = new LocalJsonStorage(path);

        try
        {
            var data = CashFlowLoader.LoadSync(storage, serializer);

            data.Expenses.Should().ContainSingle();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
