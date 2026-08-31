using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class CreditCardServiceTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger<CreditCardService> Logger = NullLogger<CreditCardService>.Instance;

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly CreditCardService _sut;

    public CreditCardServiceTests()
    {
        _repository = new StubCashFlowRepository();
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    private CreditCardService CreateService(StubCashFlowRepository? repository = null) =>
        new(repository ?? _repository, _tracer, Logger);

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new CreditCardService(null!, _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new CreditCardService(_repository, null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new CreditCardService(_repository, _tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetCreditCards_ReturnsAllSeededCards_IncludingInactive()
    {
        _repository.CreditCards.Add(CreditCard.Create("BaAmex", isActive: true));
        _repository.CreditCards.Add(CreditCard.Create("PaypalCredit", isActive: false));

        var result = _sut.GetCreditCards();

        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Name == "PaypalCredit" && !c.IsActive);
    }

    [Fact]
    public async Task CreateCreditCardAsync_WithValidRequest_AddsAndSaves()
    {
        var request = new CreditCardCreateDTO { Name = "Nubank", IsActive = true };

        var result = await _sut.CreateCreditCardAsync(request);

        using (new AssertionScope())
        {
            result.Name.Should().Be("Nubank");
            result.IsActive.Should().BeTrue();
            result.NextInvoiceDueDate.Should().BeNull();
            result.HasReferences.Should().BeFalse();
            _repository.CreditCards.Should().ContainSingle(c => c.Name == "Nubank");
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateCreditCardAsync_WithoutAName_ThrowsAndWritesNothing(string? name)
    {
        var request = new CreditCardCreateDTO { Name = name!, IsActive = true };

        var act = async () => await _sut.CreateCreditCardAsync(request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task CreateCreditCardAsync_WithDuplicateName_ThrowsAndWritesNothing()
    {
        _repository.CreditCards.Add(CreditCard.Create("Nubank", isActive: true));
        var request = new CreditCardCreateDTO { Name = "Nubank", IsActive = true };

        var act = async () => await _sut.CreateCreditCardAsync(request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<DuplicateNameException>();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task UpdateCreditCardAsync_ExistingId_ReturnsUpdatedDtoAndPersists()
    {
        var card = CreditCard.Create("BaAmex", isActive: true);
        _repository.CreditCards.Add(card);
        var dueDate = new DateOnly(2026, 9, 5);

        var result = await _sut.UpdateCreditCardAsync(card.Id, new CreditCardUpdateDTO
        {
            Name = "Nubank",
            NextInvoiceDueDate = dueDate,
            IsActive = false
        });

        using (new AssertionScope())
        {
            result.Name.Should().Be("Nubank");
            result.NextInvoiceDueDate.Should().Be(dueDate);
            result.IsActive.Should().BeFalse();
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task UpdateCreditCardAsync_ToItsOwnCurrentName_Succeeds()
    {
        var card = CreditCard.Create("BaAmex", isActive: true);
        _repository.CreditCards.Add(card);

        var result = await _sut.UpdateCreditCardAsync(card.Id, new CreditCardUpdateDTO
        {
            Name = "BaAmex",
            NextInvoiceDueDate = null,
            IsActive = false
        });

        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCreditCardAsync_UnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.UpdateCreditCardAsync(Guid.NewGuid(), new CreditCardUpdateDTO
        {
            Name = "BaAmex",
            NextInvoiceDueDate = null,
            IsActive = true
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repository.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateCreditCardAsync_WithDuplicateName_ThrowsAndWritesNothing()
    {
        var baAmex = CreditCard.Create("BaAmex", isActive: true);
        var nubank = CreditCard.Create("Nubank", isActive: true);
        _repository.CreditCards.Add(baAmex);
        _repository.CreditCards.Add(nubank);

        var act = async () => await _sut.UpdateCreditCardAsync(baAmex.Id, new CreditCardUpdateDTO
        {
            Name = "Nubank",
            NextInvoiceDueDate = null,
            IsActive = true
        });

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<DuplicateNameException>();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateCreditCardAsync_WithoutAName_ThrowsAndWritesNothing(string? name)
    {
        var card = CreditCard.Create("BaAmex", isActive: true);
        _repository.CreditCards.Add(card);

        var act = async () => await _sut.UpdateCreditCardAsync(card.Id, new CreditCardUpdateDTO
        {
            Name = name!,
            NextInvoiceDueDate = null,
            IsActive = true
        });

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task DeleteCreditCardAsync_WithNoReferences_RemovesAndSaves()
    {
        var card = CreditCard.Create("BaAmex", isActive: true);
        _repository.CreditCards.Add(card);

        await _sut.DeleteCreditCardAsync(card.Id);

        using (new AssertionScope())
        {
            _repository.CreditCards.Should().BeEmpty();
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task DeleteCreditCardAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.DeleteCreditCardAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteCreditCardAsync_ReferencedByExpense_ThrowsAndWritesNothing()
    {
        var card = CreditCard.Create("BaAmex", isActive: true);
        _repository.CreditCards.Add(card);
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, Category.Create("Mercado"), null, card));

        var act = async () => await _sut.DeleteCreditCardAsync(card.Id);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<EntityInUseException>();
            _repository.CreditCards.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task DeleteCreditCardAsync_ReferencedByCardStatement_ThrowsAndWritesNothing()
    {
        var card = CreditCard.Create("BaAmex", isActive: true);
        _repository.CreditCards.Add(card);
        _repository.CardStatements.Add(CardStatement.Create(card, 2026, 7));

        var act = async () => await _sut.DeleteCreditCardAsync(card.Id);

        await act.Should().ThrowAsync<EntityInUseException>();
    }

    [Fact]
    public void GetCreditCards_ReferencedCard_HasReferencesIsTrue()
    {
        var card = CreditCard.Create("BaAmex", isActive: true);
        _repository.CreditCards.Add(card);
        _repository.CardStatements.Add(CardStatement.Create(card, 2026, 7));

        var result = _sut.GetCreditCards();

        result.Should().ContainSingle(c => c.Id == card.Id && c.HasReferences);
    }
}
