using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Application.Tests.TestHelpers;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;
using CreditCard = Financial.CashFlow.Domain.Entities.CreditCard;

namespace Financial.CashFlow.Application.Tests.Services;

public class CardStatementServiceTests
{
    private static CardStatementService NewService(StubCashFlowRepository repository) =>
        new(repository, NullLogger<CardStatementService>.Instance);

    private static CreditCard Card(StubCashFlowRepository repository, string name) =>
        repository.CreditCards.First(c => c.Name == name);

    private static MarkStatementPaidDTO PaidBy(StubCashFlowRepository repository, string? bankName) =>
        new() { PaymentSourceBankId = repository.Banks.FirstOrDefault(b => b.Name == bankName)?.Id };

    private static Expense AddCharge(
        StubCashFlowRepository repository, DateOnly date, decimal value, CreditCard card)
    {
        var expense = Expense.Create(date, "Charge", value, Category.Mercado, null, card);
        repository.Expenses.Add(expense);
        return expense;
    }

    private static Expense AddChargeWithInvoiceDate(
        StubCashFlowRepository repository, DateOnly date, DateOnly invoiceDate, decimal value, CreditCard card)
    {
        var expense = Expense.Create(date, "Charge near cutoff", value, Category.Mercado, null, card, invoiceDate);
        repository.Expenses.Add(expense);
        return expense;
    }

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new CardStatementService(null!, NullLogger<CardStatementService>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new CardStatementService(new StubCashFlowRepository(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task GetStatementsForMonthAsync_FirstCall_GeneratesExactlyFiveUnpaidStatements()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = NewService(repository);

        var result = await service.GetStatementsForMonthAsync(2026, 7);

        using (new AssertionScope())
        {
            result.Should().HaveCount(5);
            result.Should().OnlyContain(s => !s.IsPaid);
            repository.CardStatements.Should().HaveCount(5);
        }
    }

    [Fact]
    public async Task GetStatementsForMonthAsync_InactiveCard_IsExcludedFromGeneration()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        repository.CreditCards.Add(CreditCard.Create("RetiredCard", isActive: false));
        var service = NewService(repository);

        var result = await service.GetStatementsForMonthAsync(2026, 7);

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
        var service = NewService(repository);

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
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = NewService(repository);

        await service.GetStatementsForMonthAsync(2026, 7);
        var result = await service.GetStatementsForMonthAsync(2026, 7);

        using (new AssertionScope())
        {
            result.Should().HaveCount(5);
            repository.CardStatements.Should().HaveCount(5);
            repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task GetStatementsForMonthAsync_OutstandingTotalSumsThatMonthsChargesForTheCard()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        AddCharge(repository, new DateOnly(2026, 7, 10), 30m, Card(repository, "BarclaysPlatinumVisa8003"));
        AddCharge(repository, new DateOnly(2026, 7, 15), 20m, Card(repository, "BarclaysPlatinumVisa8003"));
        AddCharge(repository, new DateOnly(2026, 8, 1), 100m, Card(repository, "BarclaysPlatinumVisa8003"));
        AddCharge(repository, new DateOnly(2026, 7, 12), 999m, Card(repository, "BaAmex"));
        var service = NewService(repository);

        var result = await service.GetStatementsForMonthAsync(2026, 7);

        result.Should().ContainSingle(s => s.CreditCardName == "BarclaysPlatinumVisa8003" && s.OutstandingTotal == 50m);
    }

    [Fact]
    public async Task GetStatementsForMonthAsync_ExcludesSettledAndImmediateExpensesFromOutstandingTotal()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var barclays = repository.Banks.First(b => b.Name == "Barclays");
        var settled = AddCharge(repository, new DateOnly(2026, 7, 10), 30m, Card(repository, "BarclaysPlatinumVisa8003"));
        settled.Settle(barclays, new DateOnly(2026, 7, 20));
        AddCharge(repository, new DateOnly(2026, 7, 11), 20m, Card(repository, "BarclaysPlatinumVisa8003"));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 12), "Immediate", 5m, Category.Casa, barclays, null));
        var service = NewService(repository);

        var result = await service.GetStatementsForMonthAsync(2026, 7);

        result.Should().ContainSingle(s => s.CreditCardName == "BarclaysPlatinumVisa8003" && s.OutstandingTotal == 20m);
    }

    [Fact]
    public async Task MarkStatementPaidAsync_SettlesEveryChargeForTheCardMonthWithBankAndToday()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var first = AddCharge(repository, new DateOnly(2026, 7, 10), 30m, Card(repository, "BarclaysPlatinumVisa8003"));
        var second = AddCharge(repository, new DateOnly(2026, 7, 15), 20m, Card(repository, "BarclaysPlatinumVisa8003"));
        var otherMonth = AddCharge(repository, new DateOnly(2026, 8, 1), 100m, Card(repository, "BarclaysPlatinumVisa8003"));
        var otherCard = AddCharge(repository, new DateOnly(2026, 7, 12), 999m, Card(repository, "BaAmex"));
        var service = NewService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        var statement = repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003" && s.Month == 7);

        var result = await service.MarkStatementPaidAsync(statement.Id, PaidBy(repository, "Trading212"));

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
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var cutoffCharge = AddChargeWithInvoiceDate(
            repository,
            date: new DateOnly(2026, 7, 29),
            invoiceDate: new DateOnly(2026, 8, 1),
            value: 40m,
            card: Card(repository, "BarclaysPlatinumVisa8003"));
        var service = NewService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        await service.GetStatementsForMonthAsync(2026, 8);
        var julyStatement = repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003" && s.Month == 7);
        var augustStatement = repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003" && s.Month == 8);

        var julyResult = await service.MarkStatementPaidAsync(julyStatement.Id, PaidBy(repository, "Trading212"));

        using (new AssertionScope())
        {
            julyResult.OutstandingTotal.Should().Be(0m);
            julyResult.Warning.Should().NotBeNull();
            cutoffCharge.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
        }

        var augustResult = await service.MarkStatementPaidAsync(augustStatement.Id, PaidBy(repository, "Trading212"));

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
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = NewService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        var statement = repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003");

        var result = await service.MarkStatementPaidAsync(statement.Id, PaidBy(repository, "Trading212"));

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
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        AddCharge(repository, new DateOnly(2026, 7, 10), 30m, Card(repository, "BarclaysPlatinumVisa8003"));
        var service = NewService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        var statement = repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003");

        var result = await service.MarkStatementPaidAsync(statement.Id, PaidBy(repository, "Trading212"));

        result.Warning.Should().BeNull();
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_ChargeNearBillingCutoff_RevertsOnlyTheInvoicePeriodMatch()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var cutoffCharge = AddChargeWithInvoiceDate(
            repository,
            date: new DateOnly(2026, 7, 29),
            invoiceDate: new DateOnly(2026, 8, 1),
            value: 40m,
            card: Card(repository, "BarclaysPlatinumVisa8003"));
        var service = NewService(repository);
        await service.GetStatementsForMonthAsync(2026, 8);
        var augustStatement = repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003" && s.Month == 8);
        await service.MarkStatementPaidAsync(augustStatement.Id, PaidBy(repository, "Trading212"));

        var result = await service.UnmarkStatementPaidAsync(augustStatement.Id);

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
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var charge = AddCharge(repository, new DateOnly(2026, 7, 10), 30m, Card(repository, "BarclaysPlatinumVisa8003"));
        var service = NewService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        var statement = repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003");
        var savesBefore = repository.SaveChangesCallCount;

        var act = async () => await service.MarkStatementPaidAsync(statement.Id, PaidBy(repository, paymentSource));

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Payment source*not recognized*");
            statement.IsPaid.Should().BeFalse();
            charge.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
            repository.SaveChangesCallCount.Should().Be(savesBefore);
        }
    }

    [Fact]
    public async Task MarkStatementPaidAsync_CalledAgainOnAlreadyPaidStatement_IsANoOpThatStillSucceeds()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = NewService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        var statement = repository.CardStatements.First();
        await service.MarkStatementPaidAsync(statement.Id, PaidBy(repository, "Barclays"));
        var savesBefore = repository.SaveChangesCallCount;

        var result = await service.MarkStatementPaidAsync(statement.Id, PaidBy(repository, "Chase"));

        result.IsPaid.Should().BeTrue();
        repository.SaveChangesCallCount.Should().Be(savesBefore);
    }

    [Fact]
    public async Task MarkStatementPaidAsync_WhenSaveFails_RollsBackStatementAndCascadedExpenses()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var charge = AddCharge(repository, new DateOnly(2026, 7, 10), 30m, Card(repository, "BarclaysPlatinumVisa8003"));
        var service = NewService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        var statement = repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003");
        repository.ThrowOnNextSave = true;

        var act = async () => await service.MarkStatementPaidAsync(statement.Id, PaidBy(repository, "Barclays"));

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
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = NewService(repository);

        var act = async () => await service.MarkStatementPaidAsync(Guid.NewGuid(), PaidBy(repository, "Barclays"));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_RevertsEverySettledExpenseForTheCardMonth()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var first = AddCharge(repository, new DateOnly(2026, 7, 10), 30m, Card(repository, "BarclaysPlatinumVisa8003"));
        var second = AddCharge(repository, new DateOnly(2026, 7, 15), 20m, Card(repository, "BarclaysPlatinumVisa8003"));
        var service = NewService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        var statement = repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003");
        await service.MarkStatementPaidAsync(statement.Id, PaidBy(repository, "Barclays"));

        var result = await service.UnmarkStatementPaidAsync(statement.Id);

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
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = NewService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        var statement = repository.CardStatements.First();
        var savesBefore = repository.SaveChangesCallCount;

        var result = await service.UnmarkStatementPaidAsync(statement.Id);

        result.IsPaid.Should().BeFalse();
        repository.SaveChangesCallCount.Should().Be(savesBefore);
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_WithNoSettledExpenses_StillFlipsStatementToUnpaid()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = NewService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        var statement = repository.CardStatements.First();
        await service.MarkStatementPaidAsync(statement.Id, PaidBy(repository, "Barclays"));

        var result = await service.UnmarkStatementPaidAsync(statement.Id);

        result.IsPaid.Should().BeFalse();
        statement.IsPaid.Should().BeFalse();
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_WhenSaveFails_RollsBackStatementAndCascadedExpenses()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var charge = AddCharge(repository, new DateOnly(2026, 7, 10), 30m, Card(repository, "BarclaysPlatinumVisa8003"));
        var service = NewService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        var statement = repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003");
        await service.MarkStatementPaidAsync(statement.Id, PaidBy(repository, "Trading212"));
        var paymentDate = charge.Date;
        repository.ThrowOnNextSave = true;

        var act = async () => await service.UnmarkStatementPaidAsync(statement.Id);

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
        var service = NewService(new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true));

        var act = async () => await service.UnmarkStatementPaidAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

}
