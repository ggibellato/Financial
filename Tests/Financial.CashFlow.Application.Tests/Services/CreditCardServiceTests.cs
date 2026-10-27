using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

[Trait("Category", "Unit")]
public class CreditCardServiceTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger<CreditCardService> Logger = NullLogger<CreditCardService>.Instance;

    private readonly StubCashFlowRepository _repository;
    private readonly FakeCreditCardCalendarSyncService _calendarSyncTrigger;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly CreditCardService _sut;

    public CreditCardServiceTests()
    {
        _repository = new StubCashFlowRepository();
        _calendarSyncTrigger = new FakeCreditCardCalendarSyncService();
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    private CreditCardService CreateService(StubCashFlowRepository? repository = null) =>
        new(repository ?? _repository, _calendarSyncTrigger, _tracer, Logger);

    [Fact]
    public void GetCreditCards_ReturnsAllSeededCards_IncludingInactive()
    {
        _repository.Accounts.Add(TestAccounts.CreditCard("BaAmex", isActive: true));
        _repository.Accounts.Add(TestAccounts.CreditCard("PaypalCredit", isActive: false));

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
            _repository.CardAccounts.Should().ContainSingle(c => c.Name == "Nubank");
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
        _repository.Accounts.Add(TestAccounts.CreditCard("Nubank", isActive: true));
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
        var card = TestAccounts.CreditCard("BaAmex", isActive: true);
        _repository.Accounts.Add(card);
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
        var card = TestAccounts.CreditCard("BaAmex", isActive: true);
        _repository.Accounts.Add(card);

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
        var baAmex = TestAccounts.CreditCard("BaAmex", isActive: true);
        var nubank = TestAccounts.CreditCard("Nubank", isActive: true);
        _repository.Accounts.Add(baAmex);
        _repository.Accounts.Add(nubank);

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
        var card = TestAccounts.CreditCard("BaAmex", isActive: true);
        _repository.Accounts.Add(card);

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
        var card = TestAccounts.CreditCard("BaAmex", isActive: true);
        _repository.Accounts.Add(card);

        await _sut.DeleteCreditCardAsync(card.Id);

        using (new AssertionScope())
        {
            _repository.CardAccounts.Should().BeEmpty();
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
        var card = TestAccounts.CreditCard("BaAmex", isActive: true);
        _repository.Accounts.Add(card);
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, Category.Create("Mercado"), null, card));

        var act = async () => await _sut.DeleteCreditCardAsync(card.Id);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<EntityInUseException>();
            _repository.CardAccounts.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task DeleteCreditCardAsync_ReferencedByCardStatement_ThrowsAndWritesNothing()
    {
        var card = TestAccounts.CreditCard("BaAmex", isActive: true);
        _repository.Accounts.Add(card);
        _repository.CardStatements.Add(CardStatement.Create(card, 2026, 7));

        var act = async () => await _sut.DeleteCreditCardAsync(card.Id);

        await act.Should().ThrowAsync<EntityInUseException>();
    }

    [Fact]
    public void GetCreditCards_ReferencedCard_HasReferencesIsTrue()
    {
        var card = TestAccounts.CreditCard("BaAmex", isActive: true);
        _repository.Accounts.Add(card);
        _repository.CardStatements.Add(CardStatement.Create(card, 2026, 7));

        var result = _sut.GetCreditCards();

        result.Should().ContainSingle(c => c.Id == card.Id && c.HasReferences);
    }

    [Fact]
    public void GetCreditCards_CardWithNoExpenses_LatestInvoiceDateIsNull()
    {
        var card = TestAccounts.CreditCard("BaAmex", isActive: true);
        _repository.Accounts.Add(card);

        var result = _sut.GetCreditCards();

        result.Should().ContainSingle(c => c.Id == card.Id && c.LatestInvoiceDate == null);
    }

    [Fact]
    public void GetCreditCards_CardWithMultipleExpenses_LatestInvoiceDateIsTheMaxInvoiceDate()
    {
        var card = TestAccounts.CreditCard("BaAmex", isActive: true);
        _repository.Accounts.Add(card);
        var category = Category.Create("Mercado");
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, category, null, card, new DateOnly(2026, 9, 1)));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 20), "Amazon", 20m, category, null, card, new DateOnly(2026, 8, 1)));

        var result = _sut.GetCreditCards();

        result.Should().ContainSingle(c => c.Id == card.Id && c.LatestInvoiceDate == new DateOnly(2026, 9, 1));
    }

    [Fact]
    public void GetCreditCards_ExpenseOnAnotherCard_DoesNotAffectLatestInvoiceDate()
    {
        var card = TestAccounts.CreditCard("BaAmex", isActive: true);
        var otherCard = TestAccounts.CreditCard("Nubank", isActive: true);
        _repository.Accounts.Add(card);
        _repository.Accounts.Add(otherCard);
        var category = Category.Create("Mercado");
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, category, null, otherCard, new DateOnly(2026, 12, 1)));

        var result = _sut.GetCreditCards();

        result.Should().ContainSingle(c => c.Id == card.Id && c.LatestInvoiceDate == null);
    }

    [Fact]
    public async Task CreateCreditCardAsync_TriggersCalendarSyncForTheNewCard()
    {
        var result = await _sut.CreateCreditCardAsync(new CreditCardCreateDTO { Name = "BaAmex", IsActive = true });

        _calendarSyncTrigger.TriggeredCreditCardIds.Should().ContainSingle().Which.Should().Be(result.Id);
    }

    [Fact]
    public async Task UpdateCreditCardAsync_TriggersCalendarSyncForTheUpdatedCard()
    {
        var card = TestAccounts.CreditCard("BaAmex", isActive: true);
        _repository.Accounts.Add(card);

        await _sut.UpdateCreditCardAsync(card.Id, new CreditCardUpdateDTO { Name = "BaAmex", IsActive = true, NextInvoiceDueDate = new DateOnly(2026, 9, 10) });

        _calendarSyncTrigger.TriggeredCreditCardIds.Should().ContainSingle().Which.Should().Be(card.Id);
    }

    [Fact]
    public async Task DeleteCreditCardAsync_TriggersCalendarSyncForTheDeletedCardId()
    {
        var card = TestAccounts.CreditCard("BaAmex", isActive: true);
        _repository.Accounts.Add(card);

        await _sut.DeleteCreditCardAsync(card.Id);

        _calendarSyncTrigger.TriggeredCreditCardIds.Should().ContainSingle().Which.Should().Be(card.Id);
    }

    [Fact]
    public void GetCreditCards_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnNextRead = new InvalidOperationException("Simulated read failure.");

        Action act = () => _sut.GetCreditCards();

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.CreditCardService.GetCreditCards");
    }

    [Fact]
    public async Task CreateCreditCardAsync_BuildsAnInactiveGbpCreditCardAccountWhenAsked()
    {
        await _sut.CreateCreditCardAsync(new CreditCardCreateDTO { Name = "Nubank", IsActive = false });

        var card = _repository.Accounts.Should().ContainSingle().Which;
        using (new AssertionScope())
        {
            card.Template.Should().Be(AccountTemplate.CreditCard);
            card.Currency.Should().Be(Currency.GBP);
            card.IsActive.Should().BeFalse();
            card.OpeningBalance.Should().BeNull();
        }
    }

    [Fact]
    public async Task CreateCreditCardAsync_NameOfABankInAnotherCase_ThrowsTheAccountMessageAndWritesNothing()
    {
        _repository.Accounts.Add(TestAccounts.Bank("Barclays"));

        var act = async () => await _sut.CreateCreditCardAsync(new CreditCardCreateDTO { Name = "barclays", IsActive = true });

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<DuplicateNameException>().WithMessage("An account named \"barclays\" already exists.");
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<DuplicateNameException>("CashFlow.CreditCardService.CreateCreditCard");
        }
    }

    [Fact]
    public async Task UpdateCreditCardAsync_BankId_ThrowsKeyNotFoundException()
    {
        var bank = TestAccounts.Bank("Barclays");
        _repository.Accounts.Add(bank);

        var act = async () => await _sut.UpdateCreditCardAsync(bank.Id, new CreditCardUpdateDTO { Name = "Barclays", IsActive = true });

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.CreditCardService.UpdateCreditCard");
    }

    [Fact]
    public async Task DeleteCreditCardAsync_LinkedToAnInvestmentAccount_ThrowsAndWritesNothing()
    {
        var card = TestAccounts.CreditCard("BaAmex");
        _repository.Accounts.Add(card);
        _repository.InvestmentAccounts.Add(InvestmentAccount.Create("Amex balance", true, true, InvestmentAccountSource.CreditCard, card));

        var act = async () => await _sut.DeleteCreditCardAsync(card.Id);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<EntityInUseException>()
                .WithMessage("Cannot delete a credit card that is still referenced by a statement or expense.");
            _repository.CardAccounts.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<EntityInUseException>("CashFlow.CreditCardService.DeleteCreditCard");
        }
    }

    [Fact]
    public void GetCreditCards_CardLinkedOnlyToAnInvestmentAccount_HasReferencesIsTrue()
    {
        var card = TestAccounts.CreditCard("BaAmex");
        _repository.Accounts.Add(card);
        _repository.InvestmentAccounts.Add(InvestmentAccount.Create("Amex balance", true, true, InvestmentAccountSource.CreditCard, card));

        _sut.GetCreditCards().Should().ContainSingle().Which.HasReferences.Should().BeTrue();
    }

    private CardStatement AddStatement(Account card, int year, int month, DateOnly? dueDate = null)
    {
        var statement = CardStatement.Create(card, year, month);
        statement.SetDueDate(dueDate);
        _repository.CardStatements.Add(statement);
        return statement;
    }

    private Task<CreditCardDTO> SetDueDateAsync(Account card, DateOnly? dueDate) =>
        _sut.UpdateCreditCardAsync(card.Id, new CreditCardUpdateDTO { Name = card.Name, IsActive = true, NextInvoiceDueDate = dueDate });

    [Fact]
    [Trait("AC", "P60-F07-credit-card-template-and-card-migration-10")]
    public async Task Update_DueDate_WritesItOnTheStatementOfThatMonth()
    {
        var card = TestAccounts.CreditCard("BaAmex");
        _repository.Accounts.Add(card);
        var october = AddStatement(card, 2026, 10);

        var result = await SetDueDateAsync(card, new DateOnly(2026, 11, 18));

        var november = _repository.CardStatements.Single(s => s.IsFor(card.Id, 2026, 11));
        using (new AssertionScope())
        {
            november.DueDate.Should().Be(new DateOnly(2026, 11, 18));
            november.IsPaid.Should().BeFalse();
            october.DueDate.Should().BeNull();
            result.NextInvoiceDueDate.Should().Be(new DateOnly(2026, 11, 18));
            _sut.GetCreditCards().Single().NextInvoiceDueDate.Should().Be(new DateOnly(2026, 11, 18));
        }
    }

    [Fact]
    public async Task Update_DueDateInAMonthWithAStatement_SetsItWithoutAddingOne()
    {
        var card = TestAccounts.CreditCard("BaAmex");
        _repository.Accounts.Add(card);
        var november = AddStatement(card, 2026, 11, new DateOnly(2026, 11, 10));
        november.MarkLegacyPaid();

        await SetDueDateAsync(card, new DateOnly(2026, 11, 18));

        using (new AssertionScope())
        {
            _repository.CardStatements.Should().ContainSingle();
            november.DueDate.Should().Be(new DateOnly(2026, 11, 18));
            november.IsPaid.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Update_EarlierDueDate_ClearsLaterDatesSoTheCardTakesTheNewDate()
    {
        var card = TestAccounts.CreditCard("BaAmex");
        _repository.Accounts.Add(card);
        var september = AddStatement(card, 2026, 9, new DateOnly(2026, 9, 18));
        var december = AddStatement(card, 2026, 12, new DateOnly(2026, 12, 18));

        var result = await SetDueDateAsync(card, new DateOnly(2026, 11, 18));

        using (new AssertionScope())
        {
            result.NextInvoiceDueDate.Should().Be(new DateOnly(2026, 11, 18));
            december.DueDate.Should().BeNull();
            september.DueDate.Should().Be(new DateOnly(2026, 9, 18));
        }
    }

    [Fact]
    [Trait("AC", "P60-F07-credit-card-template-and-card-migration-10")]
    public async Task Update_NullDueDate_ClearsTheCardsDatesSoItHasNone()
    {
        var card = TestAccounts.CreditCard("BaAmex");
        var other = TestAccounts.CreditCard("Nubank");
        _repository.Accounts.AddRange([card, other]);
        AddStatement(card, 2026, 10, new DateOnly(2026, 10, 18));
        AddStatement(card, 2026, 11, new DateOnly(2026, 11, 18));
        var otherNovember = AddStatement(other, 2026, 11, new DateOnly(2026, 11, 5));

        var result = await SetDueDateAsync(card, null);

        using (new AssertionScope())
        {
            result.NextInvoiceDueDate.Should().BeNull();
            _repository.CardStatements.Where(s => s.CreditCard.Id == card.Id).Should().OnlyContain(s => s.DueDate == null);
            otherNovember.DueDate.Should().Be(new DateOnly(2026, 11, 5));
            _repository.CardStatements.Should().HaveCount(3);
        }
    }
}
