using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Services;

public class MensaisServiceTests
{
    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new MensaisService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public async Task CreateBillAsync_WithValidRequest_SavesAndReturnsBill()
    {
        var repository = new StubCashFlowRepository();
        var service = new MensaisService(repository);

        var result = await service.CreateBillAsync(ValidBrasilRequest());

        result.Description.Should().Be("INSS");
        result.Area.Should().Be("Brasil");
        result.Status.Should().Be("Unset");
        repository.Bills.Should().ContainSingle();
        repository.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateBillAsync_NeverSetsNitOrMinimumWage_ThoseAreImportOnly()
    {
        var repository = new StubCashFlowRepository();
        var service = new MensaisService(repository);

        var result = await service.CreateBillAsync(ValidBrasilRequest());

        result.NitNumber.Should().BeNull();
        result.MinimumWageValue.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public async Task CreateBillAsync_WithInvalidDueDay_Throws(int dueDay)
    {
        var service = new MensaisService(new StubCashFlowRepository());
        var request = ValidBrasilRequest();
        var invalidRequest = new CreateRecurringBillDTO
        {
            DueDay = dueDay,
            Description = request.Description,
            Value = request.Value,
            Area = request.Area,
            Note = request.Note
        };

        var act = async () => await service.CreateBillAsync(invalidRequest);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateBillAsync_WithBlankDescription_Throws()
    {
        var service = new MensaisService(new StubCashFlowRepository());

        var act = async () => await service.CreateBillAsync(new CreateRecurringBillDTO
        {
            DueDay = 10,
            Description = "   ",
            Value = 100m,
            Area = "Brasil",
            Note = string.Empty
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateBillAsync_WithUnrecognizedArea_Throws()
    {
        var service = new MensaisService(new StubCashFlowRepository());

        var act = async () => await service.CreateBillAsync(new CreateRecurringBillDTO
        {
            DueDay = 10,
            Description = "Test",
            Value = 100m,
            Area = "France",
            Note = string.Empty
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeleteBillAsync_RemovesTheBill()
    {
        var repository = new StubCashFlowRepository();
        var bill = RecurringBill.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null);
        repository.Bills.Add(bill);
        var otherBill = RecurringBill.Create(15, "Council Tax", 120m, Area.UK, string.Empty, null, null);
        repository.Bills.Add(otherBill);
        var service = new MensaisService(repository);

        await service.DeleteBillAsync(bill.Id);

        repository.Bills.Should().ContainSingle().Which.Id.Should().Be(otherBill.Id);
    }

    [Fact]
    public async Task DeleteBillAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new MensaisService(new StubCashFlowRepository());

        var act = async () => await service.DeleteBillAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public void GetBills_ReturnsAllBills()
    {
        var repository = new StubCashFlowRepository();
        repository.Bills.Add(RecurringBill.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null));
        var service = new MensaisService(repository);

        var result = service.GetBills();

        result.Should().ContainSingle(b => b.Description == "INSS");
    }

    [Fact]
    public async Task UpdateBillAsync_UpdatesStatusAndValue()
    {
        var repository = new StubCashFlowRepository();
        var bill = RecurringBill.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null);
        repository.Bills.Add(bill);
        var service = new MensaisService(repository);

        var result = await service.UpdateBillAsync(bill.Id, new UpdateRecurringBillDTO
        {
            Status = "Paid",
            Value = 900m
        });

        result.Status.Should().Be("Paid");
        result.Value.Should().Be(900m);
    }

    [Fact]
    public async Task UpdateBillAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new MensaisService(new StubCashFlowRepository());

        var act = async () => await service.UpdateBillAsync(Guid.NewGuid(), new UpdateRecurringBillDTO
        {
            Status = "Paid",
            Value = 100m
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateBillAsync_WithInvalidStatus_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository();
        var bill = RecurringBill.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null);
        repository.Bills.Add(bill);
        var service = new MensaisService(repository);

        var act = async () => await service.UpdateBillAsync(bill.Id, new UpdateRecurringBillDTO
        {
            Status = "NotAStatus",
            Value = 100m
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ResetAllToUnsetAsync_SetsEveryBillStatusBackToUnset()
    {
        var repository = new StubCashFlowRepository();
        var paidBill = RecurringBill.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null);
        paidBill.Update(BillStatus.Paid, 850m);
        var scheduledBill = RecurringBill.Create(15, "Council Tax", 120m, Area.UK, string.Empty, null, null);
        scheduledBill.Update(BillStatus.Scheduled, 120m);
        repository.Bills.Add(paidBill);
        repository.Bills.Add(scheduledBill);
        var service = new MensaisService(repository);

        var result = await service.ResetAllToUnsetAsync();

        result.Should().OnlyContain(b => b.Status == "Unset");
        repository.Bills.Should().OnlyContain(b => b.Status == BillStatus.Unset);
        repository.SaveChangesCallCount.Should().Be(1);
    }

    private static CreateRecurringBillDTO ValidBrasilRequest() => new()
    {
        DueDay = 10,
        Description = "INSS",
        Value = 850m,
        Area = "Brasil",
        Note = "Direct debit"
    };

    private sealed class StubCashFlowRepository : ICashFlowRepository
    {
        public List<RecurringBill> Bills { get; } = new();
        public int SaveChangesCallCount { get; private set; }

        public IEnumerable<Expense> GetExpenses() => Array.Empty<Expense>();
        public void AddExpense(Expense expense) { }
        public void DeleteExpense(Guid id) { }

        public IEnumerable<ReserveMovement> GetReserveMovements() => Array.Empty<ReserveMovement>();
        public void AddReserveMovement(ReserveMovement movement) { }
        public void DeleteReserveMovement(Guid id) { }

        public IEnumerable<CardStatement> GetCardStatements() => Array.Empty<CardStatement>();
        public void AddCardStatement(CardStatement statement) { }

        public IEnumerable<RecurringBill> GetRecurringBills() => Bills;
        public void AddRecurringBill(RecurringBill bill) => Bills.Add(bill);
        public void DeleteRecurringBill(Guid id) => Bills.RemoveAll(b => b.Id == id);

        public IEnumerable<MaeLedgerEntry> GetMaeLedgerEntries() => Array.Empty<MaeLedgerEntry>();
        public void AddMaeLedgerEntry(MaeLedgerEntry entry) { }
        public void DeleteMaeLedgerEntry(Guid id) { }

        public IEnumerable<InvestmentSnapshot> GetInvestmentSnapshots() => Array.Empty<InvestmentSnapshot>();
        public void AddInvestmentSnapshot(InvestmentSnapshot snapshot) { }

        public Task SaveChangesAsync()
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
