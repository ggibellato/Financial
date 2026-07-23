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
    public async Task CreateTemplateAsync_WithValidRequest_SavesAndReturnsTemplate()
    {
        var repository = new StubCashFlowRepository();
        var service = new MensaisService(repository);

        var result = await service.CreateTemplateAsync(ValidBrasilRequest());

        result.Description.Should().Be("INSS");
        result.Area.Should().Be("Brasil");
        result.IsActive.Should().BeTrue();
        repository.Templates.Should().ContainSingle();
        repository.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateTemplateAsync_UkTemplateWithoutNitOrMinimumWage_Succeeds()
    {
        var repository = new StubCashFlowRepository();
        var service = new MensaisService(repository);

        var result = await service.CreateTemplateAsync(new CreateRecurringBillTemplateDTO
        {
            DueDay = 15,
            Description = "Council Tax",
            Value = 120m,
            Area = "UK",
            Note = string.Empty,
            NitNumber = null,
            MinimumWageValue = null
        });

        result.NitNumber.Should().BeNull();
        result.MinimumWageValue.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public async Task CreateTemplateAsync_WithInvalidDueDay_Throws(int dueDay)
    {
        var service = new MensaisService(new StubCashFlowRepository());
        var request = ValidBrasilRequest();
        var invalidRequest = new CreateRecurringBillTemplateDTO
        {
            DueDay = dueDay,
            Description = request.Description,
            Value = request.Value,
            Area = request.Area,
            Note = request.Note,
            NitNumber = request.NitNumber,
            MinimumWageValue = request.MinimumWageValue
        };

        var act = async () => await service.CreateTemplateAsync(invalidRequest);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateTemplateAsync_WithBlankDescription_Throws()
    {
        var service = new MensaisService(new StubCashFlowRepository());

        var act = async () => await service.CreateTemplateAsync(new CreateRecurringBillTemplateDTO
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
    public async Task CreateTemplateAsync_WithUnrecognizedArea_Throws()
    {
        var service = new MensaisService(new StubCashFlowRepository());

        var act = async () => await service.CreateTemplateAsync(new CreateRecurringBillTemplateDTO
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
    public async Task DeleteTemplateAsync_RemovesTemplateAndItsInstancesAcrossAllMonths()
    {
        var repository = new StubCashFlowRepository();
        var template = RecurringBillTemplate.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null);
        repository.Templates.Add(template);
        var otherTemplate = RecurringBillTemplate.Create(15, "Council Tax", 120m, Area.UK, string.Empty, null, null);
        repository.Templates.Add(otherTemplate);
        var service = new MensaisService(repository);
        await service.GetInstancesForMonthAsync(2026, 7);
        await service.GetInstancesForMonthAsync(2026, 8);

        await service.DeleteTemplateAsync(template.Id);

        repository.Templates.Should().ContainSingle().Which.Id.Should().Be(otherTemplate.Id);
        repository.Instances.Should().HaveCount(2);
        repository.Instances.Should().OnlyContain(i => i.TemplateId == otherTemplate.Id);
    }

    [Fact]
    public async Task DeleteTemplateAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new MensaisService(new StubCashFlowRepository());

        var act = async () => await service.DeleteTemplateAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public void GetTemplates_ReturnsAllTemplates()
    {
        var repository = new StubCashFlowRepository();
        repository.Templates.Add(RecurringBillTemplate.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null));
        var service = new MensaisService(repository);

        var result = service.GetTemplates();

        result.Should().ContainSingle(t => t.Description == "INSS");
    }

    [Fact]
    public async Task GetInstancesForMonthAsync_FirstCall_GeneratesOneInstancePerActiveTemplate()
    {
        var repository = new StubCashFlowRepository();
        repository.Templates.Add(RecurringBillTemplate.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null));
        repository.Templates.Add(RecurringBillTemplate.Create(15, "Council Tax", 120m, Area.UK, string.Empty, null, null));
        var service = new MensaisService(repository);

        var result = await service.GetInstancesForMonthAsync(2026, 7);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(i => i.Status == "Unset");
        result.Should().ContainSingle(i => i.Description == "INSS" && i.Value == 850m);
        repository.Instances.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetInstancesForMonthAsync_SecondCallSameMonth_DoesNotCreateDuplicates()
    {
        var repository = new StubCashFlowRepository();
        repository.Templates.Add(RecurringBillTemplate.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null));
        var service = new MensaisService(repository);

        await service.GetInstancesForMonthAsync(2026, 7);
        var result = await service.GetInstancesForMonthAsync(2026, 7);

        result.Should().ContainSingle();
        repository.Instances.Should().ContainSingle();
        repository.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetInstancesForMonthAsync_SkipsInactiveTemplates()
    {
        var repository = new StubCashFlowRepository();
        var inactiveTemplate = RecurringBillTemplate.Create(10, "Cancelled", 50m, Area.UK, string.Empty, null, null);
        typeof(RecurringBillTemplate).GetProperty(nameof(RecurringBillTemplate.IsActive))!
            .SetValue(inactiveTemplate, false);
        repository.Templates.Add(inactiveTemplate);
        var service = new MensaisService(repository);

        var result = await service.GetInstancesForMonthAsync(2026, 7);

        result.Should().BeEmpty();
        repository.Instances.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateInstanceAsync_UpdatesStatusAndValueWithoutTouchingTemplateOrOtherMonths()
    {
        var repository = new StubCashFlowRepository();
        var template = RecurringBillTemplate.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null);
        repository.Templates.Add(template);
        var service = new MensaisService(repository);
        await service.GetInstancesForMonthAsync(2026, 7);
        await service.GetInstancesForMonthAsync(2026, 8);
        var julyInstance = repository.Instances.Single(i => i.Month == 7);
        var augustInstance = repository.Instances.Single(i => i.Month == 8);

        var result = await service.UpdateInstanceAsync(julyInstance.Id, new UpdateRecurringBillInstanceDTO
        {
            Status = "Paid",
            Value = 900m
        });

        result.Status.Should().Be("Paid");
        result.Value.Should().Be(900m);
        template.Value.Should().Be(850m);
        augustInstance.Status.Should().Be(BillStatus.Unset);
        augustInstance.Value.Should().Be(850m);
    }

    [Fact]
    public async Task UpdateInstanceAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new MensaisService(new StubCashFlowRepository());

        var act = async () => await service.UpdateInstanceAsync(Guid.NewGuid(), new UpdateRecurringBillInstanceDTO
        {
            Status = "Paid",
            Value = 100m
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateInstanceAsync_WithInvalidStatus_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository();
        var template = RecurringBillTemplate.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null);
        repository.Templates.Add(template);
        var service = new MensaisService(repository);
        await service.GetInstancesForMonthAsync(2026, 7);
        var instance = repository.Instances.Single();

        var act = async () => await service.UpdateInstanceAsync(instance.Id, new UpdateRecurringBillInstanceDTO
        {
            Status = "NotAStatus",
            Value = 100m
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static CreateRecurringBillTemplateDTO ValidBrasilRequest() => new()
    {
        DueDay = 10,
        Description = "INSS",
        Value = 850m,
        Area = "Brasil",
        Note = "Direct debit",
        NitNumber = "12345678901",
        MinimumWageValue = 1412m
    };

    private sealed class StubCashFlowRepository : ICashFlowRepository
    {
        public List<RecurringBillTemplate> Templates { get; } = new();
        public List<RecurringBillInstance> Instances { get; } = new();
        public int SaveChangesCallCount { get; private set; }

        public IEnumerable<Expense> GetExpenses() => Array.Empty<Expense>();
        public void AddExpense(Expense expense) { }
        public void DeleteExpense(Guid id) { }

        public IEnumerable<ReserveMovement> GetReserveMovements() => Array.Empty<ReserveMovement>();
        public void AddReserveMovement(ReserveMovement movement) { }
        public void DeleteReserveMovement(Guid id) { }

        public IEnumerable<CardStatement> GetCardStatements() => Array.Empty<CardStatement>();
        public void AddCardStatement(CardStatement statement) { }

        public IEnumerable<RecurringBillTemplate> GetRecurringBillTemplates() => Templates;
        public void AddRecurringBillTemplate(RecurringBillTemplate template) => Templates.Add(template);
        public void DeleteRecurringBillTemplate(Guid id) => Templates.RemoveAll(t => t.Id == id);

        public IEnumerable<RecurringBillInstance> GetRecurringBillInstances() => Instances;
        public void AddRecurringBillInstance(RecurringBillInstance instance) => Instances.Add(instance);
        public void DeleteRecurringBillInstance(Guid id) => Instances.RemoveAll(i => i.Id == id);

        public IEnumerable<MaeLedgerEntry> GetMaeLedgerEntries() => Array.Empty<MaeLedgerEntry>();
        public void AddMaeLedgerEntry(MaeLedgerEntry entry) { }

        public IEnumerable<InvestmentSnapshot> GetInvestmentSnapshots() => Array.Empty<InvestmentSnapshot>();
        public void AddInvestmentSnapshot(InvestmentSnapshot snapshot) { }

        public Task SaveChangesAsync()
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
