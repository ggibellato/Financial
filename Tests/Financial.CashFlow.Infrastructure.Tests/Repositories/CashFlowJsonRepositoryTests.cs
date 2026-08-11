using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.CashFlow.Infrastructure.Repositories;
using Financial.Shared.Infrastructure.Persistence;
using FluentAssertions;

namespace Financial.CashFlow.Infrastructure.Tests.Repositories;

public class CashFlowJsonRepositoryTests
{
    [Fact]
    public async Task SaveChangesAsync_WritesSerializedDataThroughStorage()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cashflow-repo-{Guid.NewGuid()}.json");
        var storage = new LocalJsonStorage(path);
        var serializer = new CashFlowSerializerAdapter();
        var data = CashFlowData.Create();
        var repository = new CashFlowJsonRepository(data, storage, serializer);

        try
        {
            var bank = Bank.Create("Chase", roundUpEnabled: true);
            data.AddBank(bank);
            repository.AddExpense(Expense.Create(new DateOnly(2026, 7, 1), "Test expense", 10m, Category.Casa, bank, null));

            await repository.SaveChangesAsync();

            var written = await storage.ReadAsync();
            serializer.Deserialize(written).Expenses.Should().ContainSingle();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task SaveChangesAsync_WhenWriteFails_PropagatesException()
    {
        var invalidPath = Path.Combine(Path.GetTempPath(), $"cashflow-missing-dir-{Guid.NewGuid()}", "data.json");
        var storage = new LocalJsonStorage(invalidPath);
        var serializer = new CashFlowSerializerAdapter();
        var repository = new CashFlowJsonRepository(CashFlowData.Create(), storage, serializer);

        var act = async () => await repository.SaveChangesAsync();

        await act.Should().ThrowAsync<DirectoryNotFoundException>();
    }

    [Fact]
    public void Constructor_WithNullData_Throws()
    {
        var path = Path.GetTempFileName();
        try
        {
            Action act = () => new CashFlowJsonRepository(null!, new LocalJsonStorage(path), new CashFlowSerializerAdapter());
            act.Should().Throw<ArgumentNullException>().WithParameterName("data");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetExpenses_ReturnsAddedExpenses()
    {
        var path = Path.GetTempFileName();
        try
        {
            var data = CashFlowData.Create();
            var repository = new CashFlowJsonRepository(data, new LocalJsonStorage(path), new CashFlowSerializerAdapter());
            var expense = Expense.Create(new DateOnly(2026, 7, 1), "Test expense", 10m, Category.Casa, Bank.Create("Chase", roundUpEnabled: true), null);

            repository.AddExpense(expense);

            repository.GetExpenses().Should().ContainSingle().Which.Id.Should().Be(expense.Id);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void DeleteExpense_RemovesTheMatchingExpense()
    {
        var path = Path.GetTempFileName();
        try
        {
            var data = CashFlowData.Create();
            var repository = new CashFlowJsonRepository(data, new LocalJsonStorage(path), new CashFlowSerializerAdapter());
            var expense = Expense.Create(new DateOnly(2026, 7, 1), "Test expense", 10m, Category.Casa, Bank.Create("Chase", roundUpEnabled: true), null);
            repository.AddExpense(expense);

            repository.DeleteExpense(expense.Id);

            repository.GetExpenses().Should().BeEmpty();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void DeleteReserveMovement_RemovesTheMatchingMovement()
    {
        var path = Path.GetTempFileName();
        try
        {
            var data = CashFlowData.Create();
            var repository = new CashFlowJsonRepository(data, new LocalJsonStorage(path), new CashFlowSerializerAdapter());
            var movement = ReserveMovement.Create(ReserveBucket.Create("Investimento", 33.33m), 10m, new DateOnly(2026, 7, 1), "Test movement");
            repository.AddReserveMovement(movement);

            repository.DeleteReserveMovement(movement.Id);

            repository.GetReserveMovements().Should().BeEmpty();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetBanks_ReturnsBanksFromTheUnderlyingData()
    {
        var path = Path.GetTempFileName();
        try
        {
            var data = CashFlowData.Create();
            data.AddBank(Bank.Create("Barclays", roundUpEnabled: false));
            var repository = new CashFlowJsonRepository(data, new LocalJsonStorage(path), new CashFlowSerializerAdapter());

            repository.GetBanks().Should().ContainSingle().Which.Name.Should().Be("Barclays");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetIncomeSources_ReturnsIncomeSourcesFromTheUnderlyingData()
    {
        var path = Path.GetTempFileName();
        try
        {
            var data = CashFlowData.Create();
            data.AddIncomeSource(IncomeSource.Create("Gleison", IncomeGroup.Salary));
            var repository = new CashFlowJsonRepository(data, new LocalJsonStorage(path), new CashFlowSerializerAdapter());

            repository.GetIncomeSources().Should().ContainSingle().Which.Name.Should().Be("Gleison");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetReserveBuckets_ReturnsReserveBucketsFromTheUnderlyingData()
    {
        var path = Path.GetTempFileName();
        try
        {
            var data = CashFlowData.Create();
            data.AddReserveBucket(ReserveBucket.Create("Investimento", 33.33m));
            var repository = new CashFlowJsonRepository(data, new LocalJsonStorage(path), new CashFlowSerializerAdapter());

            repository.GetReserveBuckets().Should().ContainSingle().Which.Name.Should().Be("Investimento");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetCreditCards_ReturnsCreditCardsFromTheUnderlyingData()
    {
        var path = Path.GetTempFileName();
        try
        {
            var data = CashFlowData.Create();
            data.AddCreditCard(Domain.Entities.CreditCard.Create("Chase Freedom"));
            var repository = new CashFlowJsonRepository(data, new LocalJsonStorage(path), new CashFlowSerializerAdapter());

            repository.GetCreditCards().Should().ContainSingle().Which.Name.Should().Be("Chase Freedom");
        }
        finally
        {
            File.Delete(path);
        }
    }


    [Fact]
    public void GetInvestmentAccounts_ReturnsInvestmentAccountsFromTheUnderlyingData()
    {
        var path = Path.GetTempFileName();
        try
        {
            var data = CashFlowData.Create();
            var repository = new CashFlowJsonRepository(data, new LocalJsonStorage(path), new CashFlowSerializerAdapter());

            repository.AddInvestmentAccount(InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false));

            repository.GetInvestmentAccounts().Should().ContainSingle().Which.Name.Should().Be("ChaseSave");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
