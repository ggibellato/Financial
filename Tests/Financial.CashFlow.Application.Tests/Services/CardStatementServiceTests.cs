using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;
using CreditCard = Financial.CashFlow.Domain.Entities.CreditCard;

namespace Financial.CashFlow.Application.Tests.Services;

public class CardStatementServiceTests
{

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly CardStatementService _sut;

    public CardStatementServiceTests()
    {
        _repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    private CardStatementService CreateService(StubCashFlowRepository? repository = null) =>
        new(repository ?? _repository, NullLogger<CardStatementService>.Instance, _tracer);

    private static CreditCard Card(StubCashFlowRepository repository, string name) =>
        repository.CreditCards.First(c => c.Name == name);

    private static MarkCardStatementPaidDTO PaidBy(StubCashFlowRepository repository, string? bankName) =>
        new() { PaymentSourceBankId = repository.Banks.FirstOrDefault(b => b.Name == bankName)?.Id };

    private static Expense AddCharge(
        StubCashFlowRepository repository, DateOnly date, decimal value, CreditCard card)
    {
        var expense = Expense.Create(date, "Charge", value, Category.Create("Mercado"), null, card);
        repository.Expenses.Add(expense);
        return expense;
    }

    private static Expense AddChargeWithInvoiceDate(
        StubCashFlowRepository repository, DateOnly date, DateOnly invoiceDate, decimal value, CreditCard card)
    {
        var expense = Expense.Create(date, "Charge near cutoff", value, Category.Create("Mercado"), null, card, invoiceDate);
        repository.Expenses.Add(expense);
        return expense;
    }

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new CardStatementService(null!, NullLogger<CardStatementService>.Instance, _tracer);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new CardStatementService(_repository, null!, _tracer);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new CardStatementService(_repository, NullLogger<CardStatementService>.Instance, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public async Task GetStatementsForMonthAsync_FirstCall_GeneratesExactlyFiveUnpaidStatements()
    {
        var result = await _sut.GetStatementsForMonthAsync(2026, 7);

        using (new AssertionScope())
        {
            result.Should().HaveCount(5);
            result.Should().OnlyContain(s => !s.IsPaid);
            _repository.CardStatements.Should().HaveCount(5);
        }
    }

    [Fact]
    public async Task GetStatementsForMonthAsync_InactiveCard_IsExcludedFromGeneration()
    {
        _repository.CreditCards.Add(CreditCard.Create("RetiredCard", isActive: false));

        var result = await _sut.GetStatementsForMonthAsync(2026, 7);

        using (new AssertionScope())
        {
            result.Should().HaveCount(5);
            result.Should().NotContain(s => s.CreditCardName == "RetiredCard");
        }
    }

    [Fact]
    public async Task GetStatementsForMonthAsync_NoActiveCards_CreatesNoStatements()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = CreateService(repository);

        var result = await service.GetStatementsForMonthAsync(2026, 7);

        using (new AssertionScope())
        {
            result.Should().BeEmpty();
            repository.CardStatements.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task GetStatementsForMonthAsync_SecondCallSameMonth_DoesNotCreateDuplicates()
    {
        await _sut.GetStatementsForMonthAsync(2026, 7);
        var result = await _sut.GetStatementsForMonthAsync(2026, 7);

        using (new AssertionScope())
        {
            result.Should().HaveCount(5);
            _repository.CardStatements.Should().HaveCount(5);
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task GetStatementsForMonthAsync_OutstandingTotalSumsThatMonthsChargesForTheCard()
    {
        AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, Card(_repository, "BarclaysPlatinumVisa8003"));
        AddCharge(_repository, new DateOnly(2026, 7, 15), 20m, Card(_repository, "BarclaysPlatinumVisa8003"));
        AddCharge(_repository, new DateOnly(2026, 8, 1), 100m, Card(_repository, "BarclaysPlatinumVisa8003"));
        AddCharge(_repository, new DateOnly(2026, 7, 12), 999m, Card(_repository, "BaAmex"));

        var result = await _sut.GetStatementsForMonthAsync(2026, 7);

        result.Should().ContainSingle(s => s.CreditCardName == "BarclaysPlatinumVisa8003" && s.OutstandingTotal == 50m);
    }

    [Fact]
    public async Task GetStatementsForMonthAsync_ExcludesSettledAndImmediateExpensesFromOutstandingTotal()
    {
        var barclays = _repository.Banks.First(b => b.Name == "Barclays");
        var settled = AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, Card(_repository, "BarclaysPlatinumVisa8003"));
        settled.Settle(barclays, new DateOnly(2026, 7, 20));
        AddCharge(_repository, new DateOnly(2026, 7, 11), 20m, Card(_repository, "BarclaysPlatinumVisa8003"));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 12), "Immediate", 5m, Category.Create("Casa"), barclays, null));

        var result = await _sut.GetStatementsForMonthAsync(2026, 7);

        result.Should().ContainSingle(s => s.CreditCardName == "BarclaysPlatinumVisa8003" && s.OutstandingTotal == 20m);
    }

    [Fact]
    public async Task MarkStatementPaidAsync_SettlesEveryChargeForTheCardMonthWithBankAndToday()
    {
        var first = AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, Card(_repository, "BarclaysPlatinumVisa8003"));
        var second = AddCharge(_repository, new DateOnly(2026, 7, 15), 20m, Card(_repository, "BarclaysPlatinumVisa8003"));
        var otherMonth = AddCharge(_repository, new DateOnly(2026, 8, 1), 100m, Card(_repository, "BarclaysPlatinumVisa8003"));
        var otherCard = AddCharge(_repository, new DateOnly(2026, 7, 12), 999m, Card(_repository, "BaAmex"));
        await _sut.GetStatementsForMonthAsync(2026, 7);
        var statement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003" && s.Month == 7);

        var result = await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Trading212"));

        using (new AssertionScope())
        {
            result.IsPaid.Should().BeTrue();
            result.OutstandingTotal.Should().Be(0m);
            var today = DateOnly.FromDateTime(DateTime.Today);
            foreach (var expense in new[] { first, second })
            {
                expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardSettled);
                expense.PaymentSourceBank!.Name.Should().Be("Trading212");
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
        await _sut.GetStatementsForMonthAsync(2026, 7);
        await _sut.GetStatementsForMonthAsync(2026, 8);
        var julyStatement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003" && s.Month == 7);
        var augustStatement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003" && s.Month == 8);

        var julyResult = await _sut.MarkStatementPaidAsync(julyStatement.Id, PaidBy(_repository, "Trading212"));

        using (new AssertionScope())
        {
            julyResult.OutstandingTotal.Should().Be(0m);
            julyResult.Warning.Should().NotBeNull();
            cutoffCharge.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
        }

        var augustResult = await _sut.MarkStatementPaidAsync(augustStatement.Id, PaidBy(_repository, "Trading212"));

        using (new AssertionScope())
        {
            augustResult.OutstandingTotal.Should().Be(0m);
            augustResult.Warning.Should().BeNull();
            cutoffCharge.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardSettled);
        }
    }

    [Fact]
    public async Task MarkStatementPaidAsync_WithNoMatchingCharges_ReturnsWarningAndZeroOutstanding()
    {
        await _sut.GetStatementsForMonthAsync(2026, 7);
        var statement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003");

        var result = await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Trading212"));

        using (new AssertionScope())
        {
            result.IsPaid.Should().BeTrue();
            result.OutstandingTotal.Should().Be(0m);
            result.Warning.Should().NotBeNull();
            result.Warning.Should().Contain("2026-07");
        }
    }

    [Fact]
    public async Task MarkStatementPaidAsync_WithMatchingCharges_WarningIsNull()
    {
        AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, Card(_repository, "BarclaysPlatinumVisa8003"));
        await _sut.GetStatementsForMonthAsync(2026, 7);
        var statement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003");

        var result = await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Trading212"));

        result.Warning.Should().BeNull();
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
        await _sut.GetStatementsForMonthAsync(2026, 8);
        var augustStatement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003" && s.Month == 8);
        await _sut.MarkStatementPaidAsync(augustStatement.Id, PaidBy(_repository, "Trading212"));

        var result = await _sut.UnmarkStatementPaidAsync(augustStatement.Id);

        using (new AssertionScope())
        {
            result.IsPaid.Should().BeFalse();
            cutoffCharge.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
            cutoffCharge.PaymentSourceBank.Should().BeNull();
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
        await _sut.GetStatementsForMonthAsync(2026, 7);
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
    public async Task MarkStatementPaidAsync_CalledAgainOnAlreadyPaidStatement_IsANoOpThatStillSucceeds()
    {
        await _sut.GetStatementsForMonthAsync(2026, 7);
        var statement = _repository.CardStatements.First();
        await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Barclays"));
        var savesBefore = _repository.SaveChangesCallCount;

        var result = await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Chase"));

        using (new AssertionScope())
        {
            result.IsPaid.Should().BeTrue();
            _repository.SaveChangesCallCount.Should().Be(savesBefore);
            // Without this the caller cannot tell a no-op from a change: the same DTO came back for
            // both, so a click that did nothing reported the same success as one that did.
            result.Warning.Should().NotBeNull();
            result.Warning.Should().Contain("already marked paid");
        }
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_CalledOnAStatementThatWasNotPaid_WarnsThatNothingChanged()
    {
        await _sut.GetStatementsForMonthAsync(2026, 7);
        var statement = _repository.CardStatements.First();
        var savesBefore = _repository.SaveChangesCallCount;

        var result = await _sut.UnmarkStatementPaidAsync(statement.Id);

        using (new AssertionScope())
        {
            result.IsPaid.Should().BeFalse();
            _repository.SaveChangesCallCount.Should().Be(savesBefore);
            result.Warning.Should().NotBeNull();
            result.Warning.Should().Contain("not marked paid");
        }
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_OnAPaidStatement_ReportsNoWarning()
    {
        await _sut.GetStatementsForMonthAsync(2026, 7);
        var statement = _repository.CardStatements.First();
        await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Barclays"));

        var result = await _sut.UnmarkStatementPaidAsync(statement.Id);

        result.Warning.Should().BeNull("the call did what was asked, so there is nothing to report");
    }

    [Fact]
    public async Task MarkStatementPaidAsync_WhenSaveFails_RollsBackStatementAndCascadedExpenses()
    {
        var charge = AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, Card(_repository, "BarclaysPlatinumVisa8003"));
        await _sut.GetStatementsForMonthAsync(2026, 7);
        var statement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003");
        _repository.ThrowOnNextSave = true;

        var act = async () => await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Barclays"));

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<InvalidOperationException>();
            statement.IsPaid.Should().BeFalse();
            charge.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
            charge.PaymentSourceBank.Should().BeNull();
            charge.Date.Should().Be(charge.ChargeDate);
        }
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
        await _sut.GetStatementsForMonthAsync(2026, 7);
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
                expense.PaymentSourceBank.Should().BeNull();
                expense.Date.Should().Be(expense.ChargeDate);
            }
        }
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_OnAlreadyUnpaidStatement_IsANoOpThatStillSucceeds()
    {
        await _sut.GetStatementsForMonthAsync(2026, 7);
        var statement = _repository.CardStatements.First();
        var savesBefore = _repository.SaveChangesCallCount;

        var result = await _sut.UnmarkStatementPaidAsync(statement.Id);

        result.IsPaid.Should().BeFalse();
        _repository.SaveChangesCallCount.Should().Be(savesBefore);
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_WithNoSettledExpenses_StillFlipsStatementToUnpaid()
    {
        await _sut.GetStatementsForMonthAsync(2026, 7);
        var statement = _repository.CardStatements.First();
        await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Barclays"));

        var result = await _sut.UnmarkStatementPaidAsync(statement.Id);

        result.IsPaid.Should().BeFalse();
        statement.IsPaid.Should().BeFalse();
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_WhenSaveFails_RollsBackStatementAndCascadedExpenses()
    {
        var charge = AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, Card(_repository, "BarclaysPlatinumVisa8003"));
        await _sut.GetStatementsForMonthAsync(2026, 7);
        var statement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003");
        await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Trading212"));
        var paymentDate = charge.Date;
        _repository.ThrowOnNextSave = true;

        var act = async () => await _sut.UnmarkStatementPaidAsync(statement.Id);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<InvalidOperationException>();
            statement.IsPaid.Should().BeTrue();
            charge.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardSettled);
            charge.PaymentSourceBank!.Name.Should().Be("Trading212");
            charge.Date.Should().Be(paymentDate);
        }
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.UnmarkStatementPaidAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

}
