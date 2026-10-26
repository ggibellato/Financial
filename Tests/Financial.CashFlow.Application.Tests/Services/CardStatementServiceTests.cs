using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

[Trait("Category", "Unit")]
public class CardStatementServiceTests
{
    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly CardStatementService _sut;

    public CardStatementServiceTests()
    {
        _repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        _tracer = new RecordingTelemetryTracer();
        _sut = new CardStatementService(_repository, TestClock.At(), NullLogger<CardStatementService>.Instance, _tracer);
    }

    private static Account Card(StubCashFlowRepository repository, string name) =>
        repository.CardAccounts.First(c => c.Name == name);

    private static MarkCardStatementPaidDTO PaidBy(StubCashFlowRepository repository, string? bankName) =>
        new() { PaymentSourceBankId = repository.Accounts.FirstOrDefault(b => b.Name == bankName)?.Id };

    private CardStatement SeedStatement(Account card, int year, int month)
    {
        var statement = CardStatement.Create(card, year, month);
        _repository.CardStatements.Add(statement);
        return statement;
    }

    private void SeedStatements(int year, int month)
    {
        foreach (var card in _repository.CardAccounts.Where(card => card.IsActive))
        {
            SeedStatement(card, year, month);
        }
    }

    private static Expense AddCharge(
        StubCashFlowRepository repository, DateOnly date, decimal value, Account card)
    {
        var expense = Expense.Create(date, "Charge", value, Category.Create("Mercado"), null, card);
        repository.Expenses.Add(expense);
        return expense;
    }

    private static Expense AddChargeWithInvoiceDate(
        StubCashFlowRepository repository, DateOnly date, DateOnly invoiceDate, decimal value, Account card)
    {
        var expense = Expense.Create(date, "Charge near cutoff", value, Category.Create("Mercado"), null, card, invoiceDate);
        repository.Expenses.Add(expense);
        return expense;
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-08")]
    public void GetStatementsForMonth_ReturnsStoredStatementsOfThatMonthOnly_AndWritesNothing()
    {
        var visa = Card(_repository, "BarclaysPlatinumVisa8003");
        var july = SeedStatement(visa, 2026, 7);
        SeedStatement(visa, 2026, 8);
        SeedStatement(Card(_repository, "BaAmex"), 2026, 6);

        var result = _sut.GetStatementsForMonth(2026, 7);

        using (new AssertionScope())
        {
            result.Select(s => s.Id).Should().Equal(july.Id);
            _repository.CardStatements.Should().HaveCount(3);
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public void GetStatementsForMonth_ActiveCardWithNoStoredStatement_IsNotCreated()
    {
        var result = _sut.GetStatementsForMonth(2026, 7);

        using (new AssertionScope())
        {
            result.Should().BeEmpty();
            _repository.CardStatements.Should().BeEmpty();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public void GetStatementsForMonth_InactiveCardsStoredStatement_IsReturned()
    {
        var retired = TestAccounts.CreditCard("RetiredCard", isActive: false);
        _repository.Accounts.Add(retired);
        SeedStatement(retired, 2026, 7);

        var result = _sut.GetStatementsForMonth(2026, 7);

        result.Should().ContainSingle(s => s.CreditCardName == "RetiredCard");
    }

    [Fact]
    public void GetStatementsForMonth_PaidStatement_HasNoOutstandingTotal()
    {
        var visa = Card(_repository, "BarclaysPlatinumVisa8003");
        AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, visa);
        SeedStatement(visa, 2026, 7).MarkPaid();

        var result = _sut.GetStatementsForMonth(2026, 7);

        result.Should().ContainSingle(s => s.IsPaid && s.OutstandingTotal == 0m);
    }

    [Fact]
    [Trait("AC", "P60-F07-credit-card-template-and-card-migration-06")]
    public void GetStatementsForMonth_OutstandingTotalSumsThatMonthsChargesForTheCard()
    {
        AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, Card(_repository, "BarclaysPlatinumVisa8003"));
        AddCharge(_repository, new DateOnly(2026, 7, 15), 20m, Card(_repository, "BarclaysPlatinumVisa8003"));
        AddCharge(_repository, new DateOnly(2026, 8, 1), 100m, Card(_repository, "BarclaysPlatinumVisa8003"));
        AddCharge(_repository, new DateOnly(2026, 7, 12), 999m, Card(_repository, "BaAmex"));

        SeedStatements(2026, 7);

        var result = _sut.GetStatementsForMonth(2026, 7);

        result.Should().ContainSingle(s => s.CreditCardName == "BarclaysPlatinumVisa8003" && s.OutstandingTotal == 50m);
    }

    [Fact]
    [Trait("AC", "P60-F07-credit-card-template-and-card-migration-06")]
    public void GetStatementsForMonth_ExcludesSettledAndImmediateExpensesFromOutstandingTotal()
    {
        var barclays = _repository.Accounts.First(b => b.Name == "Barclays");
        var settled = AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, Card(_repository, "BarclaysPlatinumVisa8003"));
        settled.Settle(barclays, new DateOnly(2026, 7, 20));
        AddCharge(_repository, new DateOnly(2026, 7, 11), 20m, Card(_repository, "BarclaysPlatinumVisa8003"));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 12), "Immediate", 5m, Category.Create("Casa"), barclays, null));

        SeedStatements(2026, 7);

        var result = _sut.GetStatementsForMonth(2026, 7);

        result.Should().ContainSingle(s => s.CreditCardName == "BarclaysPlatinumVisa8003" && s.OutstandingTotal == 20m);
    }

    [Fact]
    public void GetStatementsForMonth_AccumulatedOutstandingTotalSumsUnpaidChargesAcrossAllPeriodsForTheCard()
    {
        AddCharge(_repository, new DateOnly(2026, 6, 5), 15m, Card(_repository, "BarclaysPlatinumVisa8003"));
        AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, Card(_repository, "BarclaysPlatinumVisa8003"));
        AddCharge(_repository, new DateOnly(2026, 8, 1), 100m, Card(_repository, "BarclaysPlatinumVisa8003"));
        AddCharge(_repository, new DateOnly(2026, 7, 12), 999m, Card(_repository, "BaAmex"));

        SeedStatements(2026, 7);

        var result = _sut.GetStatementsForMonth(2026, 7);

        using (new AssertionScope())
        {
            var barclays = result.Single(s => s.CreditCardName == "BarclaysPlatinumVisa8003");
            barclays.OutstandingTotal.Should().Be(30m, "only July's charge belongs to this statement's period");
            barclays.AccumulatedOutstandingTotal.Should().Be(145m, "June, July and August unpaid charges all count toward the card's total");
        }
    }

    [Fact]
    public void GetStatementsForMonth_AccumulatedOutstandingTotalExcludesSettledChargesAndOtherCards()
    {
        var barclays = _repository.Accounts.First(b => b.Name == "Barclays");
        var settled = AddCharge(_repository, new DateOnly(2026, 6, 5), 15m, Card(_repository, "BarclaysPlatinumVisa8003"));
        settled.Settle(barclays, new DateOnly(2026, 6, 20));
        AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, Card(_repository, "BarclaysPlatinumVisa8003"));
        AddCharge(_repository, new DateOnly(2026, 7, 12), 999m, Card(_repository, "BaAmex"));

        SeedStatements(2026, 7);

        var result = _sut.GetStatementsForMonth(2026, 7);

        result.Should().ContainSingle(s => s.CreditCardName == "BarclaysPlatinumVisa8003" && s.AccumulatedOutstandingTotal == 30m);
    }

    [Fact]
    public async Task MarkStatementPaidAsync_SettlesEveryChargeForTheCardMonthWithBankAndToday()
    {
        var first = AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, Card(_repository, "BarclaysPlatinumVisa8003"));
        var second = AddCharge(_repository, new DateOnly(2026, 7, 15), 20m, Card(_repository, "BarclaysPlatinumVisa8003"));
        var otherMonth = AddCharge(_repository, new DateOnly(2026, 8, 1), 100m, Card(_repository, "BarclaysPlatinumVisa8003"));
        var otherCard = AddCharge(_repository, new DateOnly(2026, 7, 12), 999m, Card(_repository, "BaAmex"));
        SeedStatements(2026, 7);
        var statement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003" && s.Month == 7);

        var result = await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Trading212"));

        using (new AssertionScope())
        {
            result.IsPaid.Should().BeTrue();
            result.OutstandingTotal.Should().Be(0m);
            var today = TestClock.Today;
            foreach (var expense in new[] { first, second })
            {
                expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardSettled);
                expense.PaymentSourceAccount!.Name.Should().Be("Trading212");
                expense.Date.Should().Be(today);
            }

            otherMonth.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
            otherCard.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
        }
    }

    [Fact]
    public async Task MarkStatementPaidAsync_ChargeNearBillingCutoff_SettlesAgainstInvoicePeriodStatementNotChargeMonth()
    {
        var cutoffCharge = AddChargeWithInvoiceDate(
            _repository,
            date: new DateOnly(2026, 7, 29),
            invoiceDate: new DateOnly(2026, 8, 1),
            value: 40m,
            card: Card(_repository, "BarclaysPlatinumVisa8003"));
        SeedStatements(2026, 7);
        SeedStatements(2026, 8);
        var julyStatement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003" && s.Month == 7);
        var augustStatement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003" && s.Month == 8);

        var payJuly = async () => await _sut.MarkStatementPaidAsync(julyStatement.Id, PaidBy(_repository, "Trading212"));

        await payJuly.Should().ThrowAsync<ArgumentException>().WithMessage("There is nothing to pay on this statement.");
        cutoffCharge.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);

        var augustResult = await _sut.MarkStatementPaidAsync(augustStatement.Id, PaidBy(_repository, "Trading212"));

        using (new AssertionScope())
        {
            augustResult.OutstandingTotal.Should().Be(0m);
            cutoffCharge.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardSettled);
        }
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-05")]
    public async Task MarkStatementPaidAsync_WithNothingToPay_ThrowsAndSavesNothing()
    {
        SeedStatements(2026, 7);
        var statement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003");

        var act = async () => await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Trading212"));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("There is nothing to pay on this statement.");
        using (new AssertionScope())
        {
            statement.IsPaid.Should().BeFalse();
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.CardStatementService.MarkStatementPaid");
        }
    }

    [Fact]
    public async Task MarkStatementPaidAsync_ChargesNettingToZero_ThrowsNothingToPay()
    {
        var visa = Card(_repository, "BarclaysPlatinumVisa8003");
        AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, visa);
        AddCharge(_repository, new DateOnly(2026, 7, 11), -30m, visa);
        SeedStatements(2026, 7);
        var statement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003");

        var act = async () => await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Trading212"));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("There is nothing to pay on this statement.");
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_ChargeNearBillingCutoff_RevertsOnlyTheInvoicePeriodMatch()
    {
        var cutoffCharge = AddChargeWithInvoiceDate(
            _repository,
            date: new DateOnly(2026, 7, 29),
            invoiceDate: new DateOnly(2026, 8, 1),
            value: 40m,
            card: Card(_repository, "BarclaysPlatinumVisa8003"));
        SeedStatements(2026, 8);
        var augustStatement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003" && s.Month == 8);
        await _sut.MarkStatementPaidAsync(augustStatement.Id, PaidBy(_repository, "Trading212"));

        var result = await _sut.UnmarkStatementPaidAsync(augustStatement.Id);

        using (new AssertionScope())
        {
            result.IsPaid.Should().BeFalse();
            cutoffCharge.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
            cutoffCharge.PaymentSourceAccount.Should().BeNull();
            cutoffCharge.Date.Should().Be(cutoffCharge.ChargeDate);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("NotABank")]
    public async Task MarkStatementPaidAsync_WithMissingOrUnknownPaymentSource_ThrowsWithoutChangingState(string? paymentSource)
    {
        var charge = AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, Card(_repository, "BarclaysPlatinumVisa8003"));
        SeedStatements(2026, 7);
        var statement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003");
        var savesBefore = _repository.SaveChangesCallCount;

        var act = async () => await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, paymentSource));

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Payment source*not recognized*");
            statement.IsPaid.Should().BeFalse();
            charge.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
            _repository.SaveChangesCallCount.Should().Be(savesBefore);
        }
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-05")]
    public async Task MarkStatementPaidAsync_AlreadyPaidStatement_ThrowsConflictAndSavesNothing()
    {
        AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, Card(_repository, "BarclaysPlatinumVisa8003"));
        SeedStatements(2026, 7);
        var statement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003");
        statement.MarkPaid();

        var act = async () => await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Chase"));

        await act.Should().ThrowAsync<StatementPaymentConflictException>().WithMessage("This statement is already paid.");
        _repository.SaveChangesCallCount.Should().Be(0);
        _tracer.ShouldHaveFailedSpan<StatementPaymentConflictException>("CashFlow.CardStatementService.MarkStatementPaid");
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_UnpaidStatement_ThrowsConflictAndSavesNothing()
    {
        SeedStatements(2026, 7);
        var statement = _repository.CardStatements.First();

        var act = async () => await _sut.UnmarkStatementPaidAsync(statement.Id);

        await act.Should().ThrowAsync<StatementPaymentConflictException>().WithMessage("This statement is not paid.");
        using (new AssertionScope())
        {
            statement.IsPaid.Should().BeFalse();
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<StatementPaymentConflictException>("CashFlow.CardStatementService.UnmarkStatementPaid");
        }
    }

    [Fact]
    public async Task MarkStatementPaidAsync_WhenSaveFails_RecordsFailedSpanAndRethrows()
    {
        AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, Card(_repository, "BarclaysPlatinumVisa8003"));
        SeedStatements(2026, 7);
        var statement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003");
        _repository.ThrowOnNextSave = true;

        var act = async () => await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Barclays"));

        await act.Should().ThrowAsync<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.CardStatementService.MarkStatementPaid");
    }

    [Fact]
    public async Task MarkStatementPaidAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.MarkStatementPaidAsync(Guid.NewGuid(), PaidBy(_repository, "Barclays"));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_RevertsEverySettledExpenseForTheCardMonth()
    {
        var first = AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, Card(_repository, "BarclaysPlatinumVisa8003"));
        var second = AddCharge(_repository, new DateOnly(2026, 7, 15), 20m, Card(_repository, "BarclaysPlatinumVisa8003"));
        SeedStatements(2026, 7);
        var statement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003");
        await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Barclays"));

        var result = await _sut.UnmarkStatementPaidAsync(statement.Id);

        using (new AssertionScope())
        {
            result.IsPaid.Should().BeFalse();
            result.OutstandingTotal.Should().Be(50m);
            foreach (var expense in new[] { first, second })
            {
                expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
                expense.PaymentSourceAccount.Should().BeNull();
                expense.Date.Should().Be(expense.ChargeDate);
            }
        }
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_PaidStatementWithNoSettledExpenses_StillFlipsStatementToUnpaid()
    {
        SeedStatements(2026, 7);
        var statement = _repository.CardStatements.First();
        statement.MarkPaid();

        var result = await _sut.UnmarkStatementPaidAsync(statement.Id);

        result.IsPaid.Should().BeFalse();
        statement.IsPaid.Should().BeFalse();
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_WhenSaveFails_RecordsFailedSpanAndRethrows()
    {
        AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, Card(_repository, "BarclaysPlatinumVisa8003"));
        SeedStatements(2026, 7);
        var statement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003");
        await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Trading212"));
        _repository.ThrowOnNextSave = true;

        var act = async () => await _sut.UnmarkStatementPaidAsync(statement.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.CardStatementService.UnmarkStatementPaid");
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.UnmarkStatementPaidAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public void GetOutstandingTotalForPeriod_WithMatchingCharges_SumsThemAndReportsChargesPosted()
    {
        var card = Card(_repository, "BarclaysPlatinumVisa8003");
        AddChargeWithInvoiceDate(_repository, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 10), 40m, card);
        AddChargeWithInvoiceDate(_repository, new DateOnly(2026, 9, 2), new DateOnly(2026, 9, 10), 15.5m, card);

        var (total, hasChargesPosted) = _sut.GetOutstandingTotalForPeriod(card.Id, 2026, 9);

        total.Should().Be(55.5m);
        hasChargesPosted.Should().BeTrue();
    }

    [Fact]
    public void GetOutstandingTotalForPeriod_WithNoMatchingCharges_ReturnsZeroAndNoChargesPosted()
    {
        var card = Card(_repository, "BarclaysPlatinumVisa8003");

        var (total, hasChargesPosted) = _sut.GetOutstandingTotalForPeriod(card.Id, 2026, 9);

        total.Should().Be(0m);
        hasChargesPosted.Should().BeFalse();
    }

    [Fact]
    public void GetOutstandingTotalForPeriod_DoesNotRequireAnExistingCardStatementRow()
    {
        var card = Card(_repository, "BarclaysPlatinumVisa8003");
        AddChargeWithInvoiceDate(_repository, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 10), 20m, card);
        _repository.CardStatements.Should().BeEmpty();

        var (total, hasChargesPosted) = _sut.GetOutstandingTotalForPeriod(card.Id, 2026, 9);

        total.Should().Be(20m);
        hasChargesPosted.Should().BeTrue();
    }

    [Fact]
    public void GetOutstandingTotalForPeriod_PaidStatement_ReturnsZeroAndNoChargesPosted()
    {
        var card = Card(_repository, "BarclaysPlatinumVisa8003");
        AddChargeWithInvoiceDate(_repository, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 10), 20m, card);
        SeedStatement(card, 2026, 9).MarkPaid();

        var (total, hasChargesPosted) = _sut.GetOutstandingTotalForPeriod(card.Id, 2026, 9);

        total.Should().Be(0m);
        hasChargesPosted.Should().BeFalse();
    }

    [Fact]
    public void GetOutstandingTotalForPeriod_IgnoresChargesFromOtherPeriodsOrOtherCards()
    {
        var card = Card(_repository, "BarclaysPlatinumVisa8003");
        var otherCard = Card(_repository, "BarclaysPlatinumVisa6007");
        AddChargeWithInvoiceDate(_repository, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 10), 99m, card);
        AddChargeWithInvoiceDate(_repository, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 10), 99m, otherCard);

        var (total, hasChargesPosted) = _sut.GetOutstandingTotalForPeriod(card.Id, 2026, 9);

        total.Should().Be(0m);
        hasChargesPosted.Should().BeFalse();
    }

    [Fact]
    public void GetStatementsForMonth_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnNextRead = new InvalidOperationException("simulated failure");

        var act = () => _sut.GetStatementsForMonth(2026, 9);

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.CardStatementService.GetStatementsForMonth");
    }

    [Fact]
    public async Task MarkStatementPaidAsync_CardAccountAsPaymentSource_ThrowsAndSavesNothing()
    {
        var card = Card(_repository, "BarclaysPlatinumVisa8003");
        var statement = CardStatement.Create(card, 2026, 7);
        _repository.CardStatements.Add(statement);

        var act = async () => await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "BaAmex"));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("BaAmex is a card account and can't pay.");
        using (new AssertionScope())
        {
            statement.IsPaid.Should().BeFalse();
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.CardStatementService.MarkStatementPaid");
        }
    }

    [Fact]
    [Trait("AC", "P60-F07-credit-card-template-and-card-migration-06")]
    public void GetStatementsForMonth_AccumulatedOutstanding_CountsEveryUnpaidChargeOfTheCardWhateverItsMonth()
    {
        var card = Card(_repository, "BaAmex");
        AddCharge(_repository, new DateOnly(2026, 6, 10), 15m, card);
        AddCharge(_repository, new DateOnly(2026, 7, 10), 25m, card);
        AddCharge(_repository, new DateOnly(2026, 9, 10), 40m, card);
        var paid = AddCharge(_repository, new DateOnly(2026, 7, 12), 100m, card);
        paid.Settle(_repository.Accounts.First(a => a.Name == "Barclays"), new DateOnly(2026, 7, 31));

        SeedStatements(2026, 7);

        var result = _sut.GetStatementsForMonth(2026, 7);

        var amex = result.Single(s => s.CreditCardName == "BaAmex");
        using (new AssertionScope())
        {
            amex.OutstandingTotal.Should().Be(25m);
            amex.AccumulatedOutstandingTotal.Should().Be(80m);
        }
    }
}
