using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
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

    private CardStatement SeedJulyVisaStatementWithCharge()
    {
        AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, Card(_repository, "BarclaysPlatinumVisa8003"));
        SeedStatements(2026, 7);
        return _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003");
    }

    private Account Bank(string name) => _repository.Accounts.Single(account => account.Name == name);

    private static readonly DateOnly July = new(2026, 7, 1);

    private Account Visa => Card(_repository, "BarclaysPlatinumVisa8003");

    private Account AddPotsAccount(Category? category)
    {
        var potsAccount = TestAccounts.Pots("Reserva", category: category);
        potsAccount.AddPot(Pot.Create("Investimento", 100m));
        _repository.Accounts.Add(potsAccount);
        return potsAccount;
    }

    private PotOperation AddCardPotWithdrawal(Account potsAccount, Account card, decimal amount, DateOnly statementMonth)
    {
        var pot = potsAccount.Pots![0];
        var operation = PotWithdrawal.Build(
            potsAccount, pot, amount, new DateOnly(2026, 7, 10), "Flights", bank: null, Category.Create("Viagem"), Guid.NewGuid(),
            new DateOnly(2026, 10, 5), new PotWithdrawalCard(card, statementMonth));
        _repository.PotTransactions.AddRange(operation.Transactions);
        _repository.Expenses.AddRange(operation.Expenses);
        return operation;
    }

    private List<Expense> InvestmentWithdrawalsOf(PotOperation operation) =>
        _repository.Expenses.Where(expense => expense.OperationId == operation.OperationId && expense.PaymentSourceAccount != null).ToList();

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
    [Trait("AC", "P60-F07-credit-card-template-and-card-migration-06")]
    public void GetAmountDueForPeriod_SumsThatMonthsChargesForTheCard()
    {
        var visa = Card(_repository, "BarclaysPlatinumVisa8003");
        AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, visa);
        AddCharge(_repository, new DateOnly(2026, 7, 15), 20m, visa);
        AddCharge(_repository, new DateOnly(2026, 8, 1), 100m, visa);
        AddCharge(_repository, new DateOnly(2026, 7, 12), 999m, Card(_repository, "BaAmex"));

        SeedStatements(2026, 7);

        _sut.GetAmountDueForPeriod(visa.Id, 2026, 7).Should().Be(50m);
        _tracer.ShouldHaveSucceededSpan("CashFlow.CardStatementService.GetAmountDueForPeriod").Attributes[TelemetryAttributeKeys.EntityId].Should().Be(visa.Id.ToString());
    }

    [Fact]
    [Trait("AC", "P60-F07-credit-card-template-and-card-migration-06")]
    public void GetAmountDueForPeriod_ExcludesSettledAndImmediateExpenses()
    {
        var barclays = _repository.Accounts.First(b => b.Name == "Barclays");
        var visa = Card(_repository, "BarclaysPlatinumVisa8003");
        var settled = AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, visa);
        settled.Settle(barclays, new DateOnly(2026, 7, 20));
        AddCharge(_repository, new DateOnly(2026, 7, 11), 20m, visa);
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 12), "Immediate", 5m, Category.Create("Casa"), barclays, null));

        SeedStatements(2026, 7);

        _sut.GetAmountDueForPeriod(visa.Id, 2026, 7).Should().Be(20m);
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-01")]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-14")]
    public async Task MarkStatementPaidAsync_WritesOneTransferForTheStatementTotalOnToday_AndLeavesChargesUntouched()
    {
        var visa = Card(_repository, "BarclaysPlatinumVisa8003");
        var first = AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, visa);
        var second = AddCharge(_repository, new DateOnly(2026, 7, 15), 20m, visa);
        var otherMonth = AddCharge(_repository, new DateOnly(2026, 8, 1), 100m, visa);
        AddCharge(_repository, new DateOnly(2026, 7, 12), 999m, Card(_repository, "BaAmex"));
        SeedStatements(2026, 7);
        var statement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003" && s.Month == 7);

        var result = await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Trading212"));

        using (new AssertionScope())
        {
            result.IsPaid.Should().BeTrue();
            result.OutstandingTotal.Should().Be(0m);
            var payment = _repository.Transfers.Should().ContainSingle().Subject;
            payment.SourceAccount.Name.Should().Be("Trading212");
            payment.DestinationAccount.Should().BeSameAs(visa);
            payment.Amount.Should().Be(50m);
            payment.Date.Should().Be(new DateOnly(2026, 10, 5));
            payment.Note.Should().BeNull();
            statement.PaymentTransferId.Should().Be(payment.Id);
            _repository.SaveChangesCallCount.Should().Be(1);
            foreach (var (charge, date) in new[] { (first, new DateOnly(2026, 7, 10)), (second, new DateOnly(2026, 7, 15)) })
            {
                charge.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
                charge.PaymentSourceAccount.Should().BeNull();
                charge.Date.Should().Be(date);
                charge.InvoiceDate.Should().Be(new DateOnly(2026, 7, 1));
                charge.Category.Name.Should().Be("Mercado");
            }

            otherMonth.InvoiceDate.Should().Be(new DateOnly(2026, 8, 1));
            _tracer.ShouldHaveSucceededSpan("CashFlow.CardStatementService.MarkStatementPaid").Attributes[TelemetryAttributeKeys.EntityId].Should().Be(statement.Id.ToString());
        }
    }

    [Fact]
    public async Task MarkStatementPaidAsync_WithADate_DatesTheTransferThatDay()
    {
        var statement = SeedJulyVisaStatementWithCharge();

        await _sut.MarkStatementPaidAsync(
            statement.Id, new MarkCardStatementPaidDTO { PaymentSourceBankId = Bank("Chase").Id, Date = new DateOnly(2026, 8, 3) });

        _repository.Transfers.Should().ContainSingle().Which.Date.Should().Be(new DateOnly(2026, 8, 3));
    }

    [Fact]
    public async Task MarkStatementPaidAsync_FutureDate_ThrowsAndSavesNothing()
    {
        var statement = SeedJulyVisaStatementWithCharge();

        var act = async () => await _sut.MarkStatementPaidAsync(
            statement.Id, new MarkCardStatementPaidDTO { PaymentSourceBankId = Bank("Chase").Id, Date = new DateOnly(2026, 10, 6) });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Date must not be in the future.");
        using (new AssertionScope())
        {
            statement.IsPaid.Should().BeFalse();
            _repository.Transfers.Should().BeEmpty();
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.CardStatementService.MarkStatementPaid");
        }
    }

    [Fact]
    public async Task MarkStatementPaidAsync_InactiveCard_IsPaid()
    {
        var closed = TestAccounts.CreditCard("Closed card");
        _repository.Accounts.Add(closed);
        AddCharge(_repository, new DateOnly(2026, 7, 10), 25m, closed);
        var statement = SeedStatement(closed, 2026, 7);
        closed.SetActive(false);

        var result = await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Chase"));

        using (new AssertionScope())
        {
            result.IsPaid.Should().BeTrue();
            _repository.Transfers.Should().ContainSingle().Which.Amount.Should().Be(25m);
        }
    }

    [Fact]
    public async Task MarkStatementPaidAsync_ChargeNearBillingCutoff_PaysTheInvoicePeriodStatementNotTheChargeMonth()
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
            _repository.Transfers.Should().ContainSingle().Which.Amount.Should().Be(40m);
            julyStatement.IsPaid.Should().BeFalse();
            cutoffCharge.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
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
    public async Task MarkStatementPaidAsync_CreditCoversTheMonth_ThrowsNothingToPayAndSavesNothing()
    {
        var visa = Card(_repository, "BarclaysPlatinumVisa8003");
        AddCharge(_repository, new DateOnly(2026, 6, 10), -60m, visa);
        AddCharge(_repository, new DateOnly(2026, 7, 10), 50m, visa);
        SeedStatements(2026, 7);
        var statement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003");

        var act = async () => await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Trading212"));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("There is nothing to pay on this statement.");
        using (new AssertionScope())
        {
            statement.IsPaid.Should().BeFalse();
            _repository.Transfers.Should().BeEmpty();
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.CardStatementService.MarkStatementPaid");
        }
    }

    [Fact]
    public async Task MarkStatementPaidAsync_CarriedCredit_WritesTheAmountToPayNotTheCharges()
    {
        var visa = Card(_repository, "BarclaysPlatinumVisa8003");
        AddCharge(_repository, new DateOnly(2026, 6, 10), -35m, visa);
        AddCharge(_repository, new DateOnly(2026, 7, 10), 50m, visa);
        SeedStatements(2026, 7);
        var statement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003");

        await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Trading212"));

        using (new AssertionScope())
        {
            _repository.Transfers.Should().ContainSingle().Which.Amount.Should().Be(15m);
            statement.IsPaid.Should().BeTrue();
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
        statement.MarkLegacyPaid();

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
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-06")]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-14")]
    public async Task UnmarkStatementPaidAsync_TransferPaid_DeletesTheTransferInOneSave()
    {
        var first = AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, Card(_repository, "BarclaysPlatinumVisa8003"));
        var second = AddCharge(_repository, new DateOnly(2026, 7, 15), 20m, Card(_repository, "BarclaysPlatinumVisa8003"));
        SeedStatements(2026, 7);
        var statement = _repository.CardStatements.Single(s => s.CreditCard.Name == "BarclaysPlatinumVisa8003");
        await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Barclays"));
        var savesBefore = _repository.SaveChangesCallCount;

        var result = await _sut.UnmarkStatementPaidAsync(statement.Id);

        using (new AssertionScope())
        {
            result.IsPaid.Should().BeFalse();
            result.OutstandingTotal.Should().Be(50m);
            statement.PaymentTransferId.Should().BeNull();
            _repository.Transfers.Should().BeEmpty();
            _repository.SaveChangesCallCount.Should().Be(savesBefore + 1);
            _tracer.ShouldHaveSucceededSpan("CashFlow.CardStatementService.UnmarkStatementPaid").Attributes[TelemetryAttributeKeys.EntityId].Should().Be(statement.Id.ToString());
            foreach (var expense in new[] { first, second })
            {
                expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
                expense.Date.Should().Be(expense.ChargeDate);
            }
        }
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_TransferMissing_ThrowsConflictAndChangesNothing()
    {
        var statement = SeedJulyVisaStatementWithCharge();
        await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Barclays"));
        var paymentId = statement.PaymentTransferId;
        _repository.Transfers.Clear();
        var savesBefore = _repository.SaveChangesCallCount;

        var act = async () => await _sut.UnmarkStatementPaidAsync(statement.Id);

        await act.Should().ThrowAsync<StatementPaymentConflictException>()
            .WithMessage("The payment for this statement was changed elsewhere. Reload and try again.");
        using (new AssertionScope())
        {
            statement.IsPaid.Should().BeTrue();
            statement.PaymentTransferId.Should().Be(paymentId);
            _repository.SaveChangesCallCount.Should().Be(savesBefore);
            _tracer.ShouldHaveFailedSpan<StatementPaymentConflictException>("CashFlow.CardStatementService.UnmarkStatementPaid");
        }
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-06")]
    public async Task UnmarkStatementPaidAsync_LegacyPaid_RestoresEverySettledChargeToTheCard()
    {
        var visa = Card(_repository, "BarclaysPlatinumVisa8003");
        var first = AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, visa);
        var second = AddCharge(_repository, new DateOnly(2026, 7, 15), 20m, visa);
        first.Settle(Bank("Barclays"), new DateOnly(2026, 8, 4));
        second.Settle(Bank("Barclays"), new DateOnly(2026, 8, 4));
        var statement = SeedStatement(visa, 2026, 7).MarkLegacyPaid();
        var transfer = Transfer.Create(new DateOnly(2026, 8, 1), Bank("Barclays"), Bank("Chase"), 10m, null, null, TestClock.Today);
        _repository.Transfers.Add(transfer);

        var result = await _sut.UnmarkStatementPaidAsync(statement.Id);

        using (new AssertionScope())
        {
            result.IsPaid.Should().BeFalse();
            result.OutstandingTotal.Should().Be(50m);
            _repository.Transfers.Should().ContainSingle().Which.Should().BeSameAs(transfer);
            first.PaymentSourceAccount.Should().BeNull();
            first.Date.Should().Be(new DateOnly(2026, 7, 10));
            second.PaymentSourceAccount.Should().BeNull();
            second.Date.Should().Be(new DateOnly(2026, 7, 15));
            _repository.SaveChangesCallCount.Should().Be(1);
        }
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
    public void GetAmountDueForPeriod_UnpaidMonth_IsTheAmountToPayAfterCredit()
    {
        var card = Card(_repository, "BarclaysPlatinumVisa8003");
        AddChargeWithInvoiceDate(_repository, new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 1), -35m, card);
        AddChargeWithInvoiceDate(_repository, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 10), 40m, card);
        AddChargeWithInvoiceDate(_repository, new DateOnly(2026, 9, 2), new DateOnly(2026, 9, 10), 15.5m, card);

        _sut.GetAmountDueForPeriod(card.Id, 2026, 9).Should().Be(20.5m);
    }

    [Fact]
    public void GetAmountDueForPeriod_NoStoredStatement_StillComesFromTheCharges()
    {
        var card = Card(_repository, "BarclaysPlatinumVisa8003");
        AddChargeWithInvoiceDate(_repository, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 10), 20m, card);

        _sut.GetAmountDueForPeriod(card.Id, 2026, 9).Should().Be(20m);
        _repository.CardStatements.Should().BeEmpty();
    }

    [Fact]
    public void GetAmountDueForPeriod_PaidStatement_IsZero()
    {
        var card = Card(_repository, "BarclaysPlatinumVisa8003");
        AddChargeWithInvoiceDate(_repository, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 10), 20m, card);
        SeedStatement(card, 2026, 9).MarkLegacyPaid();

        _sut.GetAmountDueForPeriod(card.Id, 2026, 9).Should().Be(0m);
    }

    [Fact]
    public void GetAmountDueForPeriod_NothingDueMonth_IsNull()
    {
        var card = Card(_repository, "BarclaysPlatinumVisa8003");
        AddChargeWithInvoiceDate(_repository, new DateOnly(2026, 9, 3), new DateOnly(2026, 9, 1), 20m, card);

        _sut.GetAmountDueForPeriod(card.Id, 2026, 10).Should().BeNull();
    }

    [Fact]
    public void GetAmountDueForPeriod_UnknownCard_IsNull()
    {
        _sut.GetAmountDueForPeriod(Guid.NewGuid(), 2026, 9).Should().BeNull();
    }

    [Fact]
    public void GetAmountDueForPeriod_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        var card = Card(_repository, "BarclaysPlatinumVisa8003");
        _repository.ThrowOnNextRead = new InvalidOperationException("simulated failure");

        var act = () => _sut.GetAmountDueForPeriod(card.Id, 2026, 9);

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.CardStatementService.GetAmountDueForPeriod");
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
    public async Task MarkStatementPaidAsync_WithPotCharges_WritesOneMinusXEachInTheSameSave()
    {
                var reserva = AddPotsAccount(Category.Create("Reserva"));
        AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, Visa);
        var flights = AddCardPotWithdrawal(reserva, Visa, 120m, July);
        var hotel = AddCardPotWithdrawal(reserva, Visa, 80m, July);
        var nextMonth = AddCardPotWithdrawal(reserva, Visa, 15m, new DateOnly(2026, 8, 1));
        var statement = SeedStatement(Visa, 2026, 7);

        await _sut.MarkStatementPaidAsync(statement.Id, new MarkCardStatementPaidDTO { PaymentSourceBankId = Bank("Barclays").Id, Date = new DateOnly(2026, 8, 3) });

        using (new AssertionScope())
        {
            _repository.Transfers.Should().ContainSingle(transfer => transfer.Amount == 230m);
            foreach (var (operation, amount) in new[] { (flights, 120m), (hotel, 80m) })
            {
                var withdrawal = InvestmentWithdrawalsOf(operation).Should().ContainSingle().Subject;
                withdrawal.Value.Should().Be(-amount);
                withdrawal.PaymentSourceAccount!.Name.Should().Be("Barclays");
                withdrawal.Date.Should().Be(new DateOnly(2026, 8, 3));
                withdrawal.Category.Name.Should().Be("Reserva");
                withdrawal.ReportingDate.Should().Be(July);
            }

            InvestmentWithdrawalsOf(nextMonth).Should().BeEmpty();
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task MarkStatementPaidAsync_CreditLowersTheAmountToPay_StillWritesTheFullX()
    {
                var reserva = AddPotsAccount(Category.Create("Reserva"));
        var flights = AddCardPotWithdrawal(reserva, Visa, 120m, July);
        AddCharge(_repository, new DateOnly(2026, 7, 12), -50m, Visa);
        var statement = SeedStatement(Visa, 2026, 7);

        await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Barclays"));

        using (new AssertionScope())
        {
            _repository.Transfers.Should().ContainSingle(transfer => transfer.Amount == 70m);
            InvestmentWithdrawalsOf(flights).Should().ContainSingle(withdrawal => withdrawal.Value == -120m);
        }
    }

    [Fact]
    public async Task MarkStatementPaidAsync_PotsAccountWithoutACategory_ThrowsAndSavesNothing()
    {
                var reserva = AddPotsAccount(category: null);
        AddCardPotWithdrawal(reserva, Visa, 120m, July);
        var statement = SeedStatement(Visa, 2026, 7);
        var expensesBefore = _repository.Expenses.Count;

        var act = async () => await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Barclays"));

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Set a category on Reserva first.");
            statement.IsPaid.Should().BeFalse();
            _repository.Transfers.Should().BeEmpty();
            _repository.Expenses.Should().HaveCount(expensesBefore);
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.CardStatementService.MarkStatementPaid");
        }
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_RemovesTheTransferAndEveryMinusXInOneSave()
    {
                var reserva = AddPotsAccount(Category.Create("Reserva"));
        var flights = AddCardPotWithdrawal(reserva, Visa, 120m, July);
        var statement = SeedStatement(Visa, 2026, 7);
        await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Barclays"));
        var savesBefore = _repository.SaveChangesCallCount;

        await _sut.UnmarkStatementPaidAsync(statement.Id);

        using (new AssertionScope())
        {
            statement.IsPaid.Should().BeFalse();
            _repository.Transfers.Should().BeEmpty();
            InvestmentWithdrawalsOf(flights).Should().BeEmpty();
            _repository.Expenses.Should().ContainSingle(expense => expense.OperationId == flights.OperationId && expense.Value == 120m);
            _repository.SaveChangesCallCount.Should().Be(savesBefore + 1);
        }
    }

    private CardStatement SeedNothingDueStatementWithPotCharge(out PotOperation flights, string? potsCategory = "Reserva")
    {
        var reserva = AddPotsAccount(potsCategory is null ? null : Category.Create(potsCategory));
        flights = AddCardPotWithdrawal(reserva, Visa, 120m, July);
        AddCharge(_repository, new DateOnly(2026, 7, 12), -120m, Visa);
        return SeedStatement(Visa, 2026, 7);
    }

    private SettlePotWithdrawalsDTO SettleFrom(DateOnly? date = null) =>
        new() { PaymentSourceBankId = Bank("Barclays").Id, Date = date };

    [Fact]
    public async Task SettlePotWithdrawalsAsync_NothingDue_WritesEachMinusXAndSettlesWithoutAPaymentInOneSave()
    {
        var statement = SeedNothingDueStatementWithPotCharge(out var flights);

        var result = await _sut.SettlePotWithdrawalsAsync(statement.Id, SettleFrom(new DateOnly(2026, 8, 3)));

        using (new AssertionScope())
        {
            var withdrawal = InvestmentWithdrawalsOf(flights).Should().ContainSingle().Subject;
            withdrawal.Value.Should().Be(-120m);
            withdrawal.PaymentSourceAccount!.Name.Should().Be("Barclays");
            withdrawal.Date.Should().Be(new DateOnly(2026, 8, 3));
            withdrawal.ReportingDate.Should().Be(July);
            statement.SettledOn.Should().Be(new DateOnly(2026, 8, 3));
            statement.IsPaid.Should().BeFalse();
            result.SettledOn.Should().Be(new DateOnly(2026, 8, 3));
            _repository.Transfers.Should().BeEmpty();
            _repository.SaveChangesCallCount.Should().Be(1);
            _tracer.ShouldHaveSucceededSpan("CashFlow.CardStatementService.SettlePotWithdrawals");
        }
    }

    [Fact]
    public async Task SettlePotWithdrawalsAsync_WithoutADate_SettlesToday()
    {
        var statement = SeedNothingDueStatementWithPotCharge(out _);

        await _sut.SettlePotWithdrawalsAsync(statement.Id, SettleFrom());

        statement.SettledOn.Should().Be(TestClock.Today);
    }

    [Fact]
    public async Task SettlePotWithdrawalsAsync_StatementWithAnAmountToPay_ThrowsConflictAndSavesNothing()
    {
        var reserva = AddPotsAccount(Category.Create("Reserva"));
        AddCardPotWithdrawal(reserva, Visa, 120m, July);
        var statement = SeedStatement(Visa, 2026, 7);

        var act = async () => await _sut.SettlePotWithdrawalsAsync(statement.Id, SettleFrom());

        await ShouldRefuseSettlement<StatementPaymentConflictException>(act, statement, "This statement now has an amount to pay; mark it paid instead.");
    }

    [Fact]
    public async Task SettlePotWithdrawalsAsync_NoPotWithdrawals_ThrowsAndSavesNothing()
    {
        AddCharge(_repository, new DateOnly(2026, 7, 10), 30m, Visa);
        AddCharge(_repository, new DateOnly(2026, 7, 12), -30m, Visa);
        var statement = SeedStatement(Visa, 2026, 7);

        var act = async () => await _sut.SettlePotWithdrawalsAsync(statement.Id, SettleFrom());

        await ShouldRefuseSettlement<ArgumentException>(act, statement, "This statement has no pot withdrawals to settle.");
    }

    [Fact]
    public async Task SettlePotWithdrawalsAsync_AlreadySettled_ThrowsConflict()
    {
        var statement = SeedNothingDueStatementWithPotCharge(out _);
        await _sut.SettlePotWithdrawalsAsync(statement.Id, SettleFrom());

        var act = async () => await _sut.SettlePotWithdrawalsAsync(statement.Id, SettleFrom());

        await act.Should().ThrowAsync<StatementPaymentConflictException>().WithMessage("This statement is already paid or settled.");
    }

    [Fact]
    public async Task SettlePotWithdrawalsAsync_FutureDate_ThrowsAndSavesNothing()
    {
        var statement = SeedNothingDueStatementWithPotCharge(out _);

        var act = async () => await _sut.SettlePotWithdrawalsAsync(statement.Id, SettleFrom(TestClock.Today.AddDays(1)));

        await ShouldRefuseSettlement<ArgumentException>(act, statement, "Date must not be in the future.");
    }

    [Fact]
    public async Task SettlePotWithdrawalsAsync_PotsAccountWithoutACategory_ThrowsAndSavesNothing()
    {
        var statement = SeedNothingDueStatementWithPotCharge(out _, potsCategory: null);

        var act = async () => await _sut.SettlePotWithdrawalsAsync(statement.Id, SettleFrom());

        await ShouldRefuseSettlement<ArgumentException>(act, statement, "Set a category on Reserva first.");
    }

    [Fact]
    public async Task UnsettlePotWithdrawalsAsync_RemovesEveryMinusXAndOpensTheStatementInOneSave()
    {
        var statement = SeedNothingDueStatementWithPotCharge(out var flights);
        await _sut.SettlePotWithdrawalsAsync(statement.Id, SettleFrom());
        var savesBefore = _repository.SaveChangesCallCount;

        var result = await _sut.UnsettlePotWithdrawalsAsync(statement.Id);

        using (new AssertionScope())
        {
            InvestmentWithdrawalsOf(flights).Should().BeEmpty();
            statement.SettledOn.Should().BeNull();
            result.SettledOn.Should().BeNull();
            _repository.SaveChangesCallCount.Should().Be(savesBefore + 1);
            _tracer.ShouldHaveSucceededSpan("CashFlow.CardStatementService.UnsettlePotWithdrawals");
        }
    }

    [Fact]
    public async Task UnsettlePotWithdrawalsAsync_StatementNotSettled_ThrowsConflict()
    {
        var statement = SeedNothingDueStatementWithPotCharge(out _);

        var act = async () => await _sut.UnsettlePotWithdrawalsAsync(statement.Id);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<StatementPaymentConflictException>().WithMessage("This statement is not settled.");
            _tracer.ShouldHaveFailedSpan<StatementPaymentConflictException>("CashFlow.CardStatementService.UnsettlePotWithdrawals");
        }
    }

    [Fact]
    public async Task MarkStatementPaidAsync_SettledStatement_ThrowsConflict()
    {
        var statement = SeedNothingDueStatementWithPotCharge(out _);
        await _sut.SettlePotWithdrawalsAsync(statement.Id, SettleFrom());

        var act = async () => await _sut.MarkStatementPaidAsync(statement.Id, PaidBy(_repository, "Barclays"));

        await act.Should().ThrowAsync<StatementPaymentConflictException>().WithMessage("This statement is settled; unmark it as settled first.");
    }

    private async Task ShouldRefuseSettlement<TException>(Func<Task> act, CardStatement statement, string message)
        where TException : Exception
    {
        var expensesBefore = _repository.Expenses.Count;

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<TException>().WithMessage(message);
            statement.SettledOn.Should().BeNull();
            _repository.Expenses.Should().HaveCount(expensesBefore);
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<TException>("CashFlow.CardStatementService.SettlePotWithdrawals");
        }
    }
}
