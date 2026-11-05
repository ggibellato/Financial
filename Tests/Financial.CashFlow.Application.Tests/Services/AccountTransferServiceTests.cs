using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Observability;
using Financial.Tests;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace Financial.CashFlow.Application.Tests.Services;

[Trait("Category", "Unit")]
public class AccountTransferServiceTests
{
    private static readonly DateOnly Date = new(2026, 9, 10);

    private readonly StubCashFlowRepository _repository;
    private readonly FakeTimeProvider _clock;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly RecordingLogger<AccountTransferService> _logger;
    private readonly FakeCreditCardCalendarSyncService _calendarSync;
    private readonly AccountTransferService _sut;
    private readonly Account _itau = TestAccounts.Bank("Itaú", currency: Currency.BRL);

    public AccountTransferServiceTests()
    {
        _repository = new StubCashFlowRepository(seedDefaultBanks: true);
        _repository.Accounts.Add(_itau);
        _clock = TestClock.At();
        _tracer = new RecordingTelemetryTracer();
        _logger = new RecordingLogger<AccountTransferService>();
        _calendarSync = new FakeCreditCardCalendarSyncService();
        _sut = new AccountTransferService(_repository, _clock, _tracer, _logger, _calendarSync);
    }

    private Account Named(string name) => _repository.Accounts.First(a => a.Name == name);

    private Guid IdOf(string name) => Named(name).Id;

    private AccountTransferCreateDTO CreateRequest(
        string source = "Barclays", string destination = "Chase", decimal? destinationAmount = null, DateOnly? date = null) => new()
    {
        Date = date ?? Date,
        SourceAccountId = IdOf(source),
        DestinationAccountId = IdOf(destination),
        Amount = 100m,
        DestinationAmount = destinationAmount,
        Note = "Top-up"
    };

    private AccountTransferUpdateDTO UpdateRequest(
        string source, string destination, decimal amount, decimal? destinationAmount = null) => new()
    {
        Date = Date,
        SourceAccountId = IdOf(source),
        DestinationAccountId = IdOf(destination),
        Amount = amount,
        DestinationAmount = destinationAmount,
        Note = "Corrected"
    };

    private Account AddMinimal(string name, Capability capability)
    {
        var account = Account.CreateWithCapabilities(name, Currency.GBP, new HashSet<Capability> { capability });
        _repository.Accounts.Add(account);
        return account;
    }

    private Transfer SeedTransfer(Account source, Account destination, decimal amount, decimal? destinationAmount = null)
    {
        var transfer = Transfer.Create(Date, source, destination, amount, destinationAmount, note: null, TestClock.Today);
        _repository.Transfers.Add(transfer);
        return transfer;
    }

    private void ShouldHaveRejected(string operation)
    {
        using (new AssertionScope())
        {
            _repository.Transfers.Should().BeEmpty();
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<ArgumentException>($"CashFlow.AccountTransferService.{operation}");
            _logger.Entries.Should().NotContain(entry => entry.Exception != null || entry.Level >= LogLevel.Warning);
        }
    }

    [Fact]
    public async Task AddAsync_SameCurrency_SavesAndReturnsTheAmountOnBothSides()
    {
        var result = await _sut.AddAsync(CreateRequest());

        using (new AssertionScope())
        {
            result.Date.Should().Be(Date);
            result.SourceAccountName.Should().Be("Barclays");
            result.SourceCurrency.Should().Be("GBP");
            result.DestinationAccountName.Should().Be("Chase");
            result.DestinationCurrency.Should().Be("GBP");
            result.Amount.Should().Be(100m);
            result.DestinationAmount.Should().Be(100m);
            result.Note.Should().Be("Top-up");
            _repository.Transfers.Should().ContainSingle().Which.DestinationAmount.Should().BeNull();
            _repository.SaveChangesCallCount.Should().Be(1);
            _tracer.Spans.Should().ContainSingle(span => span.Name == "CashFlow.AccountTransferService.AddTransfer")
                .Which.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
        }
    }

    [Fact]
    public async Task AddAsync_CrossCurrency_StoresAndReturnsTheDestinationAmount()
    {
        var result = await _sut.AddAsync(CreateRequest(destination: "Itaú", destinationAmount: 640.50m));

        using (new AssertionScope())
        {
            result.DestinationCurrency.Should().Be("BRL");
            result.DestinationAmount.Should().Be(640.50m);
            _repository.Transfers.Should().ContainSingle().Which.DestinationAmount.Should().Be(640.50m);
        }
    }

    [Fact]
    [Trait("AC", "P60-F03-generic-account-to-account-transfer-02")]
    public async Task AddAsync_SourceWithoutCanSendTransfers_RejectedAndNothingSaved()
    {
        AddMinimal("Receiver", Capability.CanReceiveTransfers);

        var act = () => _sut.AddAsync(CreateRequest(source: "Receiver", destination: "Barclays"));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Transfers from Receiver to Barclays are not allowed.");
        ShouldHaveRejected("AddTransfer");
    }

    [Fact]
    [Trait("AC", "P60-F03-generic-account-to-account-transfer-02")]
    public async Task AddAsync_DestinationWithoutCanReceiveTransfers_RejectedAndNothingSaved()
    {
        AddMinimal("Sender", Capability.CanSendTransfers);

        var act = () => _sut.AddAsync(CreateRequest(source: "Barclays", destination: "Sender"));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Transfers from Barclays to Sender are not allowed.");
        ShouldHaveRejected("AddTransfer");
    }

    [Fact]
    public async Task AddAsync_CrossCurrencyWithoutDestinationAmount_RejectedAndNothingSaved()
    {
        var act = () => _sut.AddAsync(CreateRequest(destination: "Itaú"));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Enter the amount received in BRL.");
        ShouldHaveRejected("AddTransfer");
    }

    [Fact]
    public async Task AddAsync_SameCurrencyWithDestinationAmount_RejectedAndNothingSaved()
    {
        var act = () => _sut.AddAsync(CreateRequest(destinationAmount: 100m));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Destination amount only applies when the currencies differ.");
        ShouldHaveRejected("AddTransfer");
    }

    [Fact]
    public async Task AddAsync_UnknownAccount_RejectedWithAccountMessage()
    {
        var unknownId = Guid.NewGuid();
        var request = new AccountTransferCreateDTO
        {
            Date = Date,
            SourceAccountId = IdOf("Barclays"),
            DestinationAccountId = unknownId,
            Amount = 100m
        };

        var act = () => _sut.AddAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage($"Account '{unknownId}' was not found.");
        ShouldHaveRejected("AddTransfer");
    }

    [Fact]
    public async Task AddAsync_DatedAfterToday_RejectedAndNothingSaved()
    {
        var act = () => _sut.AddAsync(CreateRequest(date: TestClock.Today.AddDays(1)));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Date must not be in the future.");
        ShouldHaveRejected("AddTransfer");
    }

    [Fact]
    public async Task AddAsync_WhenSaveFails_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnNextSave = true;

        var act = () => _sut.AddAsync(CreateRequest());

        await act.Should().ThrowAsync<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.AccountTransferService.AddTransfer");
    }

    [Fact]
    public async Task AddAsync_NullRequest_ThrowsAndRecordsFailedSpan()
    {
        var act = () => _sut.AddAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
        _tracer.ShouldHaveFailedSpan<ArgumentNullException>("CashFlow.AccountTransferService.AddTransfer");
    }

    [Fact]
    public async Task UpdateAsync_ExistingTransfer_UpdatesInPlace()
    {
        var transfer = SeedTransfer(Named("Barclays"), Named("Chase"), 50m);

        var result = await _sut.UpdateAsync(transfer.Id, UpdateRequest("Chase", "Itaú", 80m, destinationAmount: 512.40m));

        using (new AssertionScope())
        {
            result.Id.Should().Be(transfer.Id);
            transfer.SourceAccount.Name.Should().Be("Chase");
            transfer.DestinationAccount.Name.Should().Be("Itaú");
            transfer.Amount.Should().Be(80m);
            transfer.DestinationAmount.Should().Be(512.40m);
            transfer.Note.Should().Be("Corrected");
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task UpdateAsync_OntoSameCurrencyPair_ClearsTheDestinationAmount()
    {
        var transfer = SeedTransfer(Named("Barclays"), _itau, 100m, destinationAmount: 640.50m);

        var result = await _sut.UpdateAsync(transfer.Id, UpdateRequest("Barclays", "Chase", 100m));

        using (new AssertionScope())
        {
            transfer.DestinationAmount.Should().BeNull();
            result.DestinationAmount.Should().Be(100m);
        }
    }

    [Fact]
    public async Task UpdateAsync_PairNotAllowed_KeepsTheStoredTransfer()
    {
        var transfer = SeedTransfer(Named("Barclays"), Named("Chase"), 50m);
        AddMinimal("Receiver", Capability.CanReceiveTransfers);

        var act = () => _sut.UpdateAsync(transfer.Id, UpdateRequest("Receiver", "Chase", 50m));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Transfers from Receiver to Chase are not allowed.");
        using (new AssertionScope())
        {
            transfer.SourceAccount.Name.Should().Be("Barclays");
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.AccountTransferService.UpdateTransfer");
        }
    }

    [Fact]
    public async Task UpdateAsync_UnknownTransfer_ThrowsKeyNotFound()
    {
        var act = () => _sut.UpdateAsync(Guid.NewGuid(), UpdateRequest("Barclays", "Chase", 50m));

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.AccountTransferService.UpdateTransfer");
    }

    [Fact]
    public async Task DeleteAsync_ExistingTransfer_RemovesAndSaves()
    {
        var transfer = SeedTransfer(Named("Barclays"), Named("Chase"), 50m);

        await _sut.DeleteAsync(transfer.Id);

        using (new AssertionScope())
        {
            _repository.Transfers.Should().BeEmpty();
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task DeleteAsync_UnknownTransfer_ThrowsKeyNotFound()
    {
        var act = () => _sut.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.AccountTransferService.DeleteTransfer");
    }

    [Fact]
    public void GetDestinations_Bank_ListsOtherActiveAccountsByName()
    {
        _repository.Accounts.Add(Account.CreateWithCapabilities(
            "Closed", Currency.GBP, AccountTemplates.CapabilitiesOf(AccountTemplate.Bank), isActive: false));

        var result = _sut.GetDestinations(IdOf("Barclays"));

        using (new AssertionScope())
        {
            result.Select(option => option.Name).Should().Equal("Chase", "Itaú", "Trading212");
            result.Single(option => option.Name == "Itaú").Currency.Should().Be("BRL");
            result.Single(option => option.Name == "Itaú").Id.Should().Be(_itau.Id);
        }
    }

    [Fact]
    public void GetDestinations_AccountThatCannotSend_ReturnsEmpty()
    {
        var receiver = AddMinimal("Receiver", Capability.CanReceiveTransfers);

        _sut.GetDestinations(receiver.Id).Should().BeEmpty();
    }

    [Fact]
    public void GetSources_Bank_ListsOtherActiveAccountsByName()
    {
        var result = _sut.GetSources(_itau.Id);

        result.Select(option => option.Name).Should().Equal("Barclays", "Chase", "Trading212");
    }

    [Fact]
    public void GetSources_AccountThatCannotReceive_ReturnsEmpty()
    {
        var sender = AddMinimal("Sender", Capability.CanSendTransfers);

        _sut.GetSources(sender.Id).Should().BeEmpty();
    }

    [Fact]
    public void GetDestinations_UnknownAccount_ThrowsKeyNotFound()
    {
        var act = () => _sut.GetDestinations(Guid.NewGuid());

        act.Should().Throw<KeyNotFoundException>();
        _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.AccountTransferService.GetTransferDestinations");
    }

    [Fact]
    public void GetSources_UnknownAccount_ThrowsKeyNotFound()
    {
        var act = () => _sut.GetSources(Guid.NewGuid());

        act.Should().Throw<KeyNotFoundException>();
        _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.AccountTransferService.GetTransferSources");
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-10")]
    public async Task UpdateAsync_StatementPayment_ThrowsConflictAndChangesNothing()
    {
        var payment = SeedStatementPayment();

        var act = () => _sut.UpdateAsync(payment.Id, UpdateRequest("Barclays", "Chase", 50m));

        await act.Should().ThrowAsync<StatementPaymentConflictException>()
            .WithMessage("This transfer pays a card statement; unmark the statement instead.");
        using (new AssertionScope())
        {
            payment.Amount.Should().Be(40m);
            payment.DestinationAccount.Name.Should().Be("Amex");
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<StatementPaymentConflictException>("CashFlow.AccountTransferService.UpdateTransfer");
        }
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-10")]
    public async Task DeleteAsync_StatementPayment_ThrowsConflictAndKeepsIt()
    {
        var payment = SeedStatementPayment();

        var act = () => _sut.DeleteAsync(payment.Id);

        await act.Should().ThrowAsync<StatementPaymentConflictException>()
            .WithMessage("This transfer pays a card statement; unmark the statement instead.");
        using (new AssertionScope())
        {
            _repository.Transfers.Should().ContainSingle().Which.Should().BeSameAs(payment);
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<StatementPaymentConflictException>("CashFlow.AccountTransferService.DeleteTransfer");
        }
    }

    [Fact]
    public void GetDestinations_FromABank_NeverOffersACard()
    {
        _repository.Accounts.Add(TestAccounts.CreditCard("Amex"));

        _sut.GetDestinations(IdOf("Barclays")).Select(option => option.Name).Should().NotContain("Amex");
    }

    private Transfer SeedStatementPayment() => TestTransfers.SeedStatementPayment(_repository, Named("Barclays"), Date);

    private Account SeedCardWithCredit(decimal credit)
    {
        var card = TestAccounts.CreditCard("Amex");
        _repository.Accounts.Add(card);
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 8, 5), "Refund", -credit, Category.Create("Mercado"), null, card));
        return card;
    }

    private Task<AccountTransferDTO> AddCreditRefund(decimal amount, decimal? destinationAmount = null, string destination = "Barclays", DateOnly? date = null) =>
        _sut.AddCreditRefundAsync(new AccountTransferCreateDTO
        {
            Date = date ?? Date,
            SourceAccountId = IdOf("Amex"),
            DestinationAccountId = IdOf(destination),
            Amount = amount,
            DestinationAmount = destinationAmount,
            Note = "Issuer refund"
        });

    private async Task ShouldRefuseCreditRefund(Func<Task> act, string message)
    {
        await act.Should().ThrowAsync<ArgumentException>().WithMessage(message);
        ShouldHaveRejected("AddCreditRefund");
    }

    [Fact]
    [Trait("AC", "P60-F15-card-refunds-and-credit-05")]
    public async Task AddCreditRefundAsync_WithinCredit_WritesOneTransferOnOneSave()
    {
        SeedCardWithCredit(60m);

        var result = await AddCreditRefund(35m);

        using (new AssertionScope())
        {
            result.SourceAccountName.Should().Be("Amex");
            result.DestinationAccountName.Should().Be("Barclays");
            result.Amount.Should().Be(35m);
            result.DestinationAmount.Should().Be(35m);
            result.Note.Should().Be("Issuer refund");
            _repository.Transfers.Should().ContainSingle().Which.IsCreditRefund.Should().BeTrue();
            _repository.CardStatements.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Year = 2026, Month = 9, IsPaid = false });
            _repository.SaveChangesCallCount.Should().Be(1);
            _tracer.Spans.Should().ContainSingle(span => span.Name == "CashFlow.AccountTransferService.AddCreditRefund")
                .Which.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
        }
    }

    [Fact]
    public async Task AddCreditRefundAsync_MonthAlreadyHasAStatement_KeepsIt()
    {
        var card = SeedCardWithCredit(60m);
        var september = CardStatement.Create(card, 2026, 9);
        _repository.CardStatements.Add(september);

        await AddCreditRefund(35m);

        _repository.CardStatements.Should().ContainSingle().Which.Should().BeSameAs(september);
    }

    [Fact]
    public async Task AddCreditRefundAsync_InactiveCard_IsAllowed()
    {
        SeedCardWithCredit(60m).SetActive(false);

        await AddCreditRefund(60m);

        _repository.Transfers.Should().ContainSingle();
    }

    [Fact]
    [Trait("AC", "P60-F15-card-refunds-and-credit-05")]
    public async Task AddCreditRefundAsync_AboveTheCredit_ThrowsWithTheFigure()
    {
        SeedCardWithCredit(35m);

        await ShouldRefuseCreditRefund(() => AddCreditRefund(35.01m), "The card's credit is 35.00 GBP.");
    }

    [Fact]
    [UseCulture("pt-BR")]
    public async Task AddCreditRefundAsync_AboveTheCredit_FormatsTheFigureWithInvariantCulture()
    {
        SeedCardWithCredit(1234.5m);

        await ShouldRefuseCreditRefund(() => AddCreditRefund(2000m), "The card's credit is 1,234.50 GBP.");
    }

    [Fact]
    public async Task AddCreditRefundAsync_CardOwingMoney_ReportsNoCredit()
    {
        SeedCardWithCredit(-50m);

        await ShouldRefuseCreditRefund(() => AddCreditRefund(10m), "The card's credit is 0.00 GBP.");
    }

    [Fact]
    public async Task AddCreditRefundAsync_DatedBeforeTheCreditArose_IsRefusedByTheLedgerGuard()
    {
        SeedCardWithCredit(60m);

        await ShouldRefuseCreditRefund(
            () => AddCreditRefund(35m, date: new DateOnly(2026, 7, 15)),
            "On that date the card had less credit than this refund; choose a later date.");
    }

    [Fact]
    public async Task AddCreditRefundAsync_WithADestinationAmount_Throws()
    {
        SeedCardWithCredit(60m);

        await ShouldRefuseCreditRefund(() => AddCreditRefund(35m, destinationAmount: 35m), "A card credit refund has no destination amount.");
    }

    [Fact]
    public async Task AddCreditRefundAsync_BankInAnotherCurrency_Throws()
    {
        SeedCardWithCredit(60m);

        await ShouldRefuseCreditRefund(() => AddCreditRefund(35m, destination: "Itaú"), "The receiving account must be in the card's currency (GBP).");
    }

    [Fact]
    public async Task AddCreditRefundAsync_UnknownAccount_Throws()
    {
        var unknown = Guid.NewGuid();

        var act = () => _sut.AddCreditRefundAsync(new AccountTransferCreateDTO
        {
            Date = Date,
            SourceAccountId = unknown,
            DestinationAccountId = IdOf("Barclays"),
            Amount = 10m
        });

        await ShouldRefuseCreditRefund(act, $"Account '{unknown}' was not found.");
    }

    [Fact]
    [Trait("AC", "P60-F15-card-refunds-and-credit-05")]
    public async Task UpdateAsync_CreditRefund_ThrowsConflictAndChangesNothing()
    {
        var refund = TestTransfers.SeedCreditRefund(_repository, Named("Barclays"), Date);

        var act = () => _sut.UpdateAsync(refund.Id, UpdateRequest("Barclays", "Chase", 10m));

        await act.Should().ThrowAsync<StatementPaymentConflictException>()
            .WithMessage("This transfer refunds card credit; delete it from the card or bank screen instead.");
        using (new AssertionScope())
        {
            refund.Amount.Should().Be(40m);
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<StatementPaymentConflictException>("CashFlow.AccountTransferService.UpdateTransfer");
        }
    }

    [Fact]
    [Trait("AC", "P60-F15-card-refunds-and-credit-05")]
    public async Task DeleteAsync_CreditRefund_ThrowsConflictAndKeepsIt()
    {
        var refund = TestTransfers.SeedCreditRefund(_repository, Named("Barclays"), Date);

        var act = () => _sut.DeleteAsync(refund.Id);

        await act.Should().ThrowAsync<StatementPaymentConflictException>()
            .WithMessage("This transfer refunds card credit; delete it from the card or bank screen instead.");
        using (new AssertionScope())
        {
            _repository.Transfers.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<StatementPaymentConflictException>("CashFlow.AccountTransferService.DeleteTransfer");
        }
    }

    [Fact]
    [Trait("AC", "P60-F15-card-refunds-and-credit-05")]
    public async Task DeleteCreditRefundAsync_CreditRefund_RestoresTheCreditKeepsTheStatementAndTriggersTheCardSync()
    {
        var card = SeedCardWithCredit(60m);
        await AddCreditRefund(35m);
        var saveCountBefore = _repository.SaveChangesCallCount;

        await _sut.DeleteCreditRefundAsync(_repository.Transfers.Single().Id);

        using (new AssertionScope())
        {
            _repository.Transfers.Should().BeEmpty();
            _repository.CardStatements.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Year = 2026, Month = 9, IsPaid = false });
            _repository.SaveChangesCallCount.Should().Be(saveCountBefore + 1);
            _calendarSync.TriggeredCreditCardIds.Should().Equal(card.Id);
            _tracer.Spans.Should().ContainSingle(span => span.Name == "CashFlow.AccountTransferService.DeleteCreditRefund")
                .Which.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
        }

        var refundOfTheWholeCredit = await AddCreditRefund(60m);
        refundOfTheWholeCredit.Amount.Should().Be(60m);
    }

    [Fact]
    public async Task DeleteCreditRefundAsync_InactiveCard_IsAllowed()
    {
        var card = SeedCardWithCredit(60m);
        await AddCreditRefund(35m);
        card.SetActive(false);

        await _sut.DeleteCreditRefundAsync(_repository.Transfers.Single().Id);

        using (new AssertionScope())
        {
            _repository.Transfers.Should().BeEmpty();
            _calendarSync.TriggeredCreditCardIds.Should().Equal(card.Id);
        }
    }

    [Fact]
    public async Task DeleteCreditRefundAsync_OrdinaryTransfer_ThrowsAndKeepsIt()
    {
        var transfer = SeedTransfer(Named("Barclays"), Named("Chase"), 50m);

        var act = () => _sut.DeleteCreditRefundAsync(transfer.Id);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("This transfer is not a card credit refund.");
        using (new AssertionScope())
        {
            _repository.Transfers.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(0);
            _calendarSync.TriggeredCreditCardIds.Should().BeEmpty();
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.AccountTransferService.DeleteCreditRefund");
        }
    }

    [Fact]
    public async Task DeleteCreditRefundAsync_StatementPayment_ThrowsAndKeepsIt()
    {
        var payment = SeedStatementPayment();

        var act = () => _sut.DeleteCreditRefundAsync(payment.Id);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("This transfer is not a card credit refund.");
        _repository.Transfers.Should().ContainSingle().Which.Should().BeSameAs(payment);
    }

    [Fact]
    public async Task DeleteCreditRefundAsync_UnknownId_ThrowsKeyNotFound()
    {
        var act = () => _sut.DeleteCreditRefundAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _calendarSync.TriggeredCreditCardIds.Should().BeEmpty();
        _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.AccountTransferService.DeleteCreditRefund");
    }

    [Fact]
    public async Task DeleteCreditRefundAsync_WhenSaveFails_DoesNotTriggerTheCardSync()
    {
        var refund = TestTransfers.SeedCreditRefund(_repository, Named("Barclays"), Date);
        _repository.ThrowOnNextSave = true;

        var act = () => _sut.DeleteCreditRefundAsync(refund.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _calendarSync.TriggeredCreditCardIds.Should().BeEmpty();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.AccountTransferService.DeleteCreditRefund");
    }

    private Account SeedCard(string name, decimal balance, Currency currency = Currency.GBP, bool isActive = true)
    {
        var card = Account.CreateWithCapabilities(name, currency, AccountTemplates.CapabilitiesOf(AccountTemplate.CreditCard));
        _repository.Accounts.Add(card);
        if (balance != 0m)
        {
            _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 8, 5), "Charge", -balance, Category.Create("Mercado"), null, card));
        }

        card.SetActive(isActive);
        return card;
    }

    [Fact]
    [Trait("AC", "P60-F15-card-refunds-and-credit-05")]
    public void GetCardsInCredit_ListsEveryCardInCreditByName_InactiveAndOtherCurrenciesIncluded()
    {
        var visa = SeedCard("Visa", 35m);
        var amex = SeedCard("amex", 12.5m, isActive: false);
        var nubank = SeedCard("Nubank", 80m, Currency.BRL);
        SeedCard("Owing", -40m);
        SeedCard("Settled", 0m);

        var result = _sut.GetCardsInCredit();

        using (new AssertionScope())
        {
            result.Should().BeEquivalentTo(
                [
                    new { AccountId = amex.Id, Name = "amex", Currency = "GBP", IsActive = false, Credit = 12.5m },
                    new { AccountId = nubank.Id, Name = "Nubank", Currency = "BRL", IsActive = true, Credit = 80m },
                    new { AccountId = visa.Id, Name = "Visa", Currency = "GBP", IsActive = true, Credit = 35m }
                ],
                options => options.WithStrictOrdering());
            _tracer.Spans.Should().ContainSingle(span => span.Name == "CashFlow.AccountTransferService.GetCardsInCredit")
                .Which.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
        }
    }

    [Fact]
    public void GetCardsInCredit_CreditRefundedToABank_LowersOrRemovesTheCard()
    {
        var visa = SeedCard("Visa", 35m);
        _repository.Transfers.Add(Transfer.CreateCreditRefund(Date, visa, Named("Barclays"), 35m, null, TestClock.Today));

        _sut.GetCardsInCredit().Should().BeEmpty();
    }

    [Fact]
    public void GetCardsInCredit_NoCards_ReturnsEmpty()
    {
        _sut.GetCardsInCredit().Should().BeEmpty();
    }
}
