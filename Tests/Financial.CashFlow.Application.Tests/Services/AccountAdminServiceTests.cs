using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.ValueObjects;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

[Trait("Category", "Unit")]
public class AccountAdminServiceTests
{
    private static readonly DateOnly Today = TestClock.Today;

    private readonly StubCashFlowRepository _repository = new();
    private readonly RecordingTelemetryTracer _tracer = new();
    private readonly FakeCreditCardCalendarSyncService _calendarSyncTrigger = new();
    private readonly AccountAdminService _sut;
    private readonly Account _barclays = TestAccounts.Bank("Barclays");
    private readonly Account _chase = TestAccounts.Bank("Chase", roundUpEnabled: true);

    public AccountAdminServiceTests()
    {
        _repository.Accounts.AddRange([_barclays, _chase]);
        _repository.BalanceAdjustments.Add(BalanceAdjustment.Create(new DateOnly(2026, 9, 3), _barclays, 0m, 0m, null));
        _sut = new AccountAdminService(_repository, _calendarSyncTrigger, TestClock.At(), _tracer, NullLogger<AccountAdminService>.Instance);
    }

    private static AccountCreateDTO NewBank(
        string name = "Monzo",
        string currency = "GBP",
        decimal? openingBalance = 250m,
        DateOnly? balanceStartDate = null,
        bool? roundUpEnabled = true,
        string templateName = "Bank") => new()
    {
        TemplateName = templateName,
        Name = name,
        Currency = currency,
        OpeningBalance = openingBalance,
        BalanceStartDate = balanceStartDate ?? Today,
        RoundUpEnabled = roundUpEnabled
    };

    private static AccountUpdateDTO Edit(
        Account account,
        string? name = null,
        string currency = "GBP",
        bool isActive = true,
        decimal? openingBalance = 0m,
        bool? roundUpEnabled = false) => new()
    {
        TemplateName = "Bank",
        Name = name ?? account.Name,
        Currency = currency,
        OpeningBalance = openingBalance,
        BalanceStartDate = new DateOnly(2026, 1, 1),
        RoundUpEnabled = roundUpEnabled,
        IsActive = isActive
    };

    [Fact]
    public void GetTemplates_ListsBankAndCreditCardWithTheirCurrenciesAndCapabilitiesInOrder()
    {
        var templates = _sut.GetTemplates();

        using (new AssertionScope())
        {
            templates.Select(template => template.Name).Should().Equal("Bank", "CreditCard");
            var bank = templates[0];
            bank.Currencies.Should().Equal("GBP");
            bank.Capabilities.Should().Equal(
                "HasOpeningBalance", "ReceivesIncome", "HasCategories", "CanSendTransfers",
                "CanReceiveTransfers", "CanReconcile", "CanPayBills", "SupportsRoundUp");
            var card = templates[1];
            card.Currencies.Should().Equal("GBP");
            card.Capabilities.Should().Equal("HasCategories", "HasStatements", "ReceivesPayments");
            _tracer.Spans.Should().ContainSingle(span => span.Name == "CashFlow.AccountAdminService.GetAccountTemplates");
        }
    }

    [Fact]
    public async Task CreateAsync_ValidBank_AddsAndSavesIt()
    {
        var result = await _sut.CreateAsync(NewBank(name: "  Monzo  "));

        using (new AssertionScope())
        {
            var created = _repository.Accounts.Should().ContainSingle(account => account.Name == "Monzo").Which;
            created.Template.Should().Be(AccountTemplate.Bank);
            created.OpeningBalance.Should().Be(OpeningBalance.Create(250m, Today));
            created.RoundsUp.Should().BeTrue();
            result.Id.Should().Be(created.Id);
            result.HasRecords.Should().BeFalse();
            result.OpeningBalance.Should().Be(250m);
            result.BalanceStartDate.Should().Be(Today);
            result.RoundUpEnabled.Should().BeTrue();
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Theory]
    [InlineData("Barclays")]
    [InlineData("barclays")]
    [InlineData(" BARCLAYS ")]
    public async Task CreateAsync_DuplicateNameInAnyCase_ThrowsAndWritesNothing(string name)
    {
        var act = () => _sut.CreateAsync(NewBank(name: name));

        using (new AssertionScope())
        {
            (await act.Should().ThrowAsync<DuplicateNameException>()).WithMessage("An account named \"Barclays\" already exists.");
            _repository.Accounts.Should().HaveCount(2);
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Theory]
    [InlineData("Cash", "GBP", "Unknown account template 'Cash'.")]
    [InlineData("Bank", "EUR", "Unknown currency 'EUR'.")]
    [InlineData("Bank", "BRL", "Bank accounts must be GBP.")]
    public async Task CreateAsync_UnknownTemplateOrNotAllowedCurrency_Throws(string templateName, string currency, string message)
    {
        var act = () => _sut.CreateAsync(NewBank(templateName: templateName, currency: currency));

        using (new AssertionScope())
        {
            (await act.Should().ThrowAsync<ArgumentException>()).WithMessage(message);
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task CreateAsync_NameOverSixtyCharacters_Throws()
    {
        var act = () => _sut.CreateAsync(NewBank(name: new string('a', 61)));

        (await act.Should().ThrowAsync<ArgumentException>()).WithMessage("Account name must be 60 characters or fewer.");
    }

    [Fact]
    public async Task CreateAsync_MissingCapabilityFields_ThrowsTheFieldMessage()
    {
        var withoutOpening = () => _sut.CreateAsync(NewBank(openingBalance: null));
        var withoutStartDate = () => _sut.CreateAsync(new AccountCreateDTO
        {
            TemplateName = "Bank", Name = "Monzo", Currency = "GBP", OpeningBalance = 0m, RoundUpEnabled = false
        });
        var withoutRoundUp = () => _sut.CreateAsync(NewBank(roundUpEnabled: null));

        using (new AssertionScope())
        {
            (await withoutOpening.Should().ThrowAsync<ArgumentException>()).WithMessage("Opening balance is required.");
            (await withoutStartDate.Should().ThrowAsync<ArgumentException>()).WithMessage("Balance start date is required.");
            (await withoutRoundUp.Should().ThrowAsync<ArgumentException>()).WithMessage("Round-up is required.");
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task CreateAsync_FutureStartDate_Throws()
    {
        var act = () => _sut.CreateAsync(NewBank(balanceStartDate: Today.AddDays(1)));

        (await act.Should().ThrowAsync<ArgumentException>()).WithMessage("Balance start date can't be in the future.");
    }

    [Fact]
    public async Task CreateAsync_NegativeOpeningBalance_Throws()
    {
        var act = () => _sut.CreateAsync(NewBank(openingBalance: -1m));

        (await act.Should().ThrowAsync<ArgumentException>()).WithMessage("Opening balance cannot be negative.");
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_RenamesDeactivatesAndSaves()
    {
        var result = await _sut.UpdateAsync(_chase.Id, Edit(_chase, name: "Chase Saver", isActive: false, openingBalance: 80m));

        using (new AssertionScope())
        {
            _chase.Name.Should().Be("Chase Saver");
            _chase.IsActive.Should().BeFalse();
            _chase.OpeningBalance.Should().Be(OpeningBalance.Create(80m, new DateOnly(2026, 1, 1)));
            _chase.RoundsUp.Should().BeFalse();
            result.IsActive.Should().BeFalse();
            result.HasRecords.Should().BeFalse();
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task UpdateAsync_InactiveAccount_CanBeReactivated()
    {
        _barclays.SetActive(false);

        var result = await _sut.UpdateAsync(_barclays.Id, Edit(_barclays));

        using (new AssertionScope())
        {
            _barclays.IsActive.Should().BeTrue();
            result.HasRecords.Should().BeTrue();
        }
    }

    [Fact]
    public async Task UpdateAsync_KeepingItsOwnNameInAnotherCase_Succeeds()
    {
        await _sut.UpdateAsync(_chase.Id, Edit(_chase, name: "CHASE"));

        _chase.Name.Should().Be("CHASE");
    }

    [Fact]
    public async Task UpdateAsync_RenameToAnotherAccountInAnyCase_ThrowsAndWritesNothing()
    {
        var act = () => _sut.UpdateAsync(_chase.Id, Edit(_chase, name: "barclays"));

        using (new AssertionScope())
        {
            (await act.Should().ThrowAsync<DuplicateNameException>()).WithMessage("An account named \"barclays\" already exists.");
            _chase.Name.Should().Be("Chase");
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task UpdateAsync_CurrencyChangeWithRecords_ThrowsAndWritesNothing()
    {
        var act = () => _sut.UpdateAsync(_barclays.Id, Edit(_barclays, currency: "BRL"));

        using (new AssertionScope())
        {
            (await act.Should().ThrowAsync<ArgumentException>())
                .WithMessage("Template and currency can't change once the account has transactions.");
            _barclays.Currency.Should().Be(Currency.GBP);
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.AccountAdminService.UpdateAccount");
        }
    }

    [Fact]
    public async Task UpdateAsync_MissingOrFutureCapabilityFields_ThrowsAndLeavesTheAccountUntouched()
    {
        var withoutRoundUp = () => _sut.UpdateAsync(_chase.Id, Edit(_chase, name: "Chase Saver", roundUpEnabled: null));
        var futureStart = () => _sut.UpdateAsync(_chase.Id, new AccountUpdateDTO
        {
            TemplateName = "Bank", Name = "Chase Saver", Currency = "GBP", OpeningBalance = 0m,
            BalanceStartDate = Today.AddDays(1), RoundUpEnabled = false, IsActive = true
        });

        using (new AssertionScope())
        {
            (await withoutRoundUp.Should().ThrowAsync<ArgumentException>()).WithMessage("Round-up is required.");
            (await futureStart.Should().ThrowAsync<ArgumentException>()).WithMessage("Balance start date can't be in the future.");
            _chase.Name.Should().Be("Chase");
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task UpdateAsync_CapabilityFieldTheTemplateLacks_Throws()
    {
        var wallet = Account.CreateWithCapabilities("Wallet", Currency.GBP, new HashSet<Capability>());
        _repository.Accounts.Add(wallet);
        var request = new AccountUpdateDTO
        {
            TemplateName = wallet.TemplateName, Name = "Wallet", Currency = "GBP", OpeningBalance = 10m, IsActive = true
        };

        var act = () => _sut.UpdateAsync(wallet.Id, request);

        (await act.Should().ThrowAsync<ArgumentException>()).WithMessage("Wallet has no opening balance.");
    }

    [Fact]
    public async Task UpdateAsync_RoundUpOnATemplateWithoutIt_Throws()
    {
        var wallet = Account.CreateWithCapabilities("Wallet", Currency.GBP, new HashSet<Capability>());
        _repository.Accounts.Add(wallet);
        var request = new AccountUpdateDTO
        {
            TemplateName = wallet.TemplateName, Name = "Wallet", Currency = "GBP", RoundUpEnabled = true, IsActive = true
        };

        var act = () => _sut.UpdateAsync(wallet.Id, request);

        (await act.Should().ThrowAsync<ArgumentException>()).WithMessage("Wallet does not support round-up.");
    }

    [Fact]
    public async Task UpdateAsync_CurrencyChangeWithoutRecords_IsCheckedAgainstTheTemplate()
    {
        var act = () => _sut.UpdateAsync(_chase.Id, Edit(_chase, currency: "BRL"));

        (await act.Should().ThrowAsync<ArgumentException>()).WithMessage("Bank accounts must be GBP.");
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ThrowsKeyNotFound()
    {
        var act = () => _sut.UpdateAsync(Guid.NewGuid(), Edit(_chase));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_UnusedAccount_RemovesAndSaves()
    {
        await _sut.DeleteAsync(_chase.Id);

        using (new AssertionScope())
        {
            _repository.Accounts.Should().NotContain(_chase);
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task DeleteAsync_AccountWithRecords_ThrowsEntityInUseAndKeepsIt()
    {
        var act = () => _sut.DeleteAsync(_barclays.Id);

        using (new AssertionScope())
        {
            (await act.Should().ThrowAsync<EntityInUseException>())
                .WithMessage("Barclays has transactions and cannot be deleted. Deactivate it instead.");
            _repository.Accounts.Should().Contain(_barclays);
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ThrowsKeyNotFoundAndRecordsAFailedSpan()
    {
        var act = () => _sut.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.AccountAdminService.DeleteAccount");
    }

    [Fact]
    public async Task CreateAsync_WhenTheSaveFails_RecordsAFailedSpanAndRethrows()
    {
        _repository.ThrowOnNextSave = true;

        var act = () => _sut.CreateAsync(NewBank());

        await act.Should().ThrowAsync<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.AccountAdminService.CreateAccount");
    }

    [Fact]
    public async Task CreateAsync_CreditCard_AddsAGbpCardAccountWithoutBankFields()
    {
        var result = await _sut.CreateAsync(new AccountCreateDTO { TemplateName = "CreditCard", Name = "Nubank", Currency = "GBP" });

        var card = _repository.Accounts.Single(account => account.Id == result.Id);
        using (new AssertionScope())
        {
            card.Template.Should().Be(AccountTemplate.CreditCard);
            card.OpeningBalance.Should().BeNull();
            card.RoundUp.Should().BeNull();
            result.TemplateName.Should().Be("CreditCard");
        }
    }

    [Fact]
    [Trait("AC", "P60-F07-credit-card-template-and-card-migration-05")]
    public async Task CardChanges_TriggerCalendarSync()
    {
        var created = await _sut.CreateAsync(new AccountCreateDTO { TemplateName = "CreditCard", Name = "Nubank", Currency = "GBP" });
        await _sut.UpdateAsync(created.Id, new AccountUpdateDTO { TemplateName = "CreditCard", Name = "Nubank Gold", Currency = "GBP", IsActive = false });
        await _sut.DeleteAsync(created.Id);

        _calendarSyncTrigger.TriggeredCreditCardIds.Should().Equal(created.Id, created.Id, created.Id);
    }

    [Fact]
    public async Task BankChanges_DoNotTriggerCalendarSync()
    {
        var created = await _sut.CreateAsync(NewBank());
        await _sut.UpdateAsync(_chase.Id, Edit(_chase, name: "Chase UK"));
        await _sut.DeleteAsync(created.Id);

        _calendarSyncTrigger.TriggeredCreditCardIds.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_UnusedCardChangedToBank_TriggersCalendarSyncSoItsEventIsRemoved()
    {
        var card = TestAccounts.CreditCard("Nubank", nextInvoiceDueDate: new DateOnly(2026, 11, 18));
        _repository.Accounts.Add(card);

        await _sut.UpdateAsync(card.Id, Edit(card));

        using (new AssertionScope())
        {
            card.Template.Should().Be(AccountTemplate.Bank);
            _calendarSyncTrigger.TriggeredCreditCardIds.Should().Equal(card.Id);
        }
    }

    public static TheoryData<string> CardUses => new() { "charge", "statement", "investment account" };

    [Theory]
    [MemberData(nameof(CardUses))]
    public async Task DeleteAsync_CardInUse_ThrowsTheDeactivateMessageAndKeepsIt(string use)
    {
        var card = TestAccounts.CreditCard("BA Amex");
        _repository.Accounts.Add(card);
        switch (use)
        {
            case "charge":
                _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 9, 3), "Taxi", 30m, Category.Create("Mercado"), null, card));
                break;
            case "statement":
                _repository.CardStatements.Add(CardStatement.Create(card, 2026, 9));
                break;
            default:
                _repository.InvestmentAccounts.Add(InvestmentAccount.Create("Amex balance", true, true, InvestmentAccountSource.CreditCard, card));
                break;
        }

        var act = async () => await _sut.DeleteAsync(card.Id);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<EntityInUseException>()
                .WithMessage("BA Amex has transactions and cannot be deleted. Deactivate it instead.");
            _repository.Accounts.Should().Contain(card);
            _calendarSyncTrigger.TriggeredCreditCardIds.Should().BeEmpty();
        }
    }
}
