using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

[Trait("Category", "Unit")]
public class SettlementServiceTests
{
    private static readonly DateOnly EntryDate = new(2026, 9, 17);

    private readonly StubCashFlowRepository _repository = new();
    private readonly RecordingTelemetryTracer _tracer = new();
    private readonly Account _mae = TestAccounts.Settlement("Mãe", [Currency.BRL, Currency.GBP], "Mãe");
    private readonly Account _son = TestAccounts.Settlement("Samuel", [Currency.GBP], "Samuel");

    public SettlementServiceTests()
    {
        _repository.Accounts.Add(_mae);
        _repository.Accounts.Add(_son);
    }

    private SettlementService CreateService(StubExchangeRateProvider? provider = null) =>
        new(_repository, provider ?? new StubExchangeRateProvider(0.14627391m), TestClock.At(), _tracer, NullLogger<SettlementService>.Instance);

    private static SettlementEntryCreateDTO NewEntry(string? currency = "BRL", decimal amount = 150m, string paidBy = "You", DateOnly? date = null, string description = "Farmácia") => new()
    {
        Date = date ?? EntryDate,
        Description = description,
        Note = null,
        PaidBy = paidBy,
        Currency = currency,
        Amount = amount
    };

    private static SettlementEntryUpdateDTO EntryUpdate(decimal? convertedValue, decimal amount = 180m, DateOnly? date = null, string paidBy = "Other") => new()
    {
        Date = date ?? new DateOnly(2026, 9, 16),
        Description = "Farmácia e mercado",
        Note = "metade",
        PaidBy = paidBy,
        Amount = amount,
        ConvertedValue = convertedValue
    };

    private SettlementTransaction AddStoredEntry(Account account, Currency currency, decimal? convertedValue)
    {
        var entry = SettlementTransaction.Create(account, EntryDate, "Farmácia", null, SettlementPayer.You, currency, 150m, convertedValue);
        _repository.SettlementTransactions.Add(entry);
        return entry;
    }

    [Fact]
    public async Task AddEntryAsync_TwoCurrencies_StoresTheConvertedValueAtTheDateRateToTwoDecimals()
    {
        var result = await CreateService().AddEntryAsync(_mae.Id, NewEntry(amount: 150.005m));

        var stored = _repository.SettlementTransactions.Single();
        using (new AssertionScope())
        {
            stored.Account.Should().BeSameAs(_mae);
            stored.PaidBy.Should().Be(SettlementPayer.You);
            stored.Currency.Should().Be(Currency.BRL);
            stored.Amount.Should().Be(150.01m);
            stored.ConvertedValue.Should().Be(21.94m);
            stored.Note.Should().BeEmpty();
            result.Id.Should().Be(stored.Id);
            result.AccountId.Should().Be(_mae.Id);
            result.PaidBy.Should().Be("You");
            result.Currency.Should().Be("BRL");
            result.ConvertedCurrency.Should().Be("GBP");
            result.ConvertedValue.Should().Be(21.94m);
            result.Warning.Should().BeNull();
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Theory]
    [InlineData("BRL", "No exchange rate found for 17 Sep 2026; the GBP value is empty.")]
    [InlineData("GBP", "No exchange rate found for 17 Sep 2026; the BRL value is empty.")]
    public async Task AddEntryAsync_NoRate_SavesWithoutConvertedValueAndWarnsNamingTheOtherCurrency(string currency, string warning)
    {
        var result = await CreateService(new StubExchangeRateProvider(null)).AddEntryAsync(_mae.Id, NewEntry(currency));

        _repository.SettlementTransactions.Single().ConvertedValue.Should().BeNull();
        result.ConvertedValue.Should().BeNull();
        result.Warning.Should().Be(warning);
    }

    [Fact]
    public async Task AddEntryAsync_ConvertedValueRoundsToZero_SavesItEmptyWithWarning()
    {
        var result = await CreateService(new StubExchangeRateProvider(0.00002m)).AddEntryAsync(_mae.Id, NewEntry(amount: 100m));

        _repository.SettlementTransactions.Single().ConvertedValue.Should().BeNull();
        result.Warning.Should().Be("The GBP value rounds to 0.00, so it was left empty.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("GBP")]
    public async Task AddEntryAsync_OneCurrencyAccount_UsesItsCurrencyAndAsksNoRate(string? currency)
    {
        var provider = new StubExchangeRateProvider(6m);

        var result = await CreateService(provider).AddEntryAsync(_son.Id, NewEntry(currency, paidBy: "Other"));

        using (new AssertionScope())
        {
            var stored = _repository.SettlementTransactions.Single();
            stored.Currency.Should().Be(Currency.GBP);
            stored.PaidBy.Should().Be(SettlementPayer.Other);
            stored.ConvertedValue.Should().BeNull();
            result.ConvertedCurrency.Should().BeNull();
            result.Warning.Should().BeNull();
            provider.CallCount.Should().Be(0);
        }
    }

    [Theory]
    [InlineData("Mãe", null, "Choose the currency.")]
    [InlineData("Mãe", "USX", "Unknown currency 'USX'.")]
    [InlineData("Samuel", "BRL", "Samuel doesn't use BRL.")]
    public async Task AddEntryAsync_MissingUnknownOrUnheldCurrency_RefusedAndNothingSaved(string accountName, string? currency, string message)
    {
        var account = accountName == "Mãe" ? _mae : _son;

        var act = () => CreateService().AddEntryAsync(account.Id, NewEntry(currency));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage(message);
        _repository.SettlementTransactions.Should().BeEmpty();
    }

    [Theory]
    [InlineData("you")]
    [InlineData("1")]
    [InlineData("")]
    [InlineData("Mãe")]
    public async Task AddEntryAsync_PaidByNotYouOrOther_Refused(string paidBy)
    {
        var act = () => CreateService().AddEntryAsync(_mae.Id, NewEntry(paidBy: paidBy));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Paid by must be You or Other.");
    }

    [Fact]
    public async Task AddEntryAsync_FutureDate_RefusedWithoutAskingARate()
    {
        var provider = new StubExchangeRateProvider(1m);

        var act = () => CreateService(provider).AddEntryAsync(_mae.Id, NewEntry(date: TestClock.Today.AddDays(1)));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Date must not be in the future.");
        provider.CallCount.Should().Be(0);
        _repository.SettlementTransactions.Should().BeEmpty();
    }

    [Fact]
    public async Task AddEntryAsync_BlankDescription_RefusedWithoutAskingARate()
    {
        var provider = new StubExchangeRateProvider(1m);

        var act = () => CreateService(provider).AddEntryAsync(_mae.Id, NewEntry(description: " "));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Description is required.");
        provider.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task AddEntryAsync_InactiveAccount_Refused()
    {
        _mae.SetActive(false);

        var act = () => CreateService().AddEntryAsync(_mae.Id, NewEntry());

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Mãe is inactive.");
    }

    [Fact]
    public async Task AddEntryAsync_AccountWithoutParticipants_Refused()
    {
        var bank = TestAccounts.Bank("Barclays");
        _repository.Accounts.Add(bank);

        var act = () => CreateService().AddEntryAsync(bank.Id, NewEntry("GBP"));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Barclays is not a Settlement account.");
    }

    [Fact]
    public async Task AddEntryAsync_UnknownAccount_ThrowsKeyNotFound()
    {
        var act = () => CreateService().AddEntryAsync(Guid.NewGuid(), NewEntry());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task AddEntryAsync_Success_RecordsSuccessfulSpan()
    {
        await CreateService().AddEntryAsync(_mae.Id, NewEntry());

        _tracer.Spans.Should().ContainSingle(s => s.Name == "CashFlow.SettlementService.AddEntry")
            .Which.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
    }

    [Fact]
    public async Task AddEntryAsync_Invalid_RecordsFailedSpan()
    {
        var act = () => CreateService().AddEntryAsync(_mae.Id, NewEntry(amount: 0m));

        await act.Should().ThrowAsync<ArgumentException>();
        _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.SettlementService.AddEntry");
    }

    [Fact]
    public async Task UpdateEntryAsync_ChangesDetailsAndValues_KeepsCurrencyAndDoesNotRecalculateTheConvertedValue()
    {
        var entry = AddStoredEntry(_mae, Currency.BRL, 21.94m);
        var provider = new StubExchangeRateProvider(1m);

        var result = await CreateService(provider).UpdateEntryAsync(_mae.Id, entry.Id, EntryUpdate(25.6m));

        using (new AssertionScope())
        {
            entry.Date.Should().Be(new DateOnly(2026, 9, 16));
            entry.Description.Should().Be("Farmácia e mercado");
            entry.Note.Should().Be("metade");
            entry.PaidBy.Should().Be(SettlementPayer.Other);
            entry.Amount.Should().Be(180m);
            entry.ConvertedValue.Should().Be(25.6m);
            entry.Currency.Should().Be(Currency.BRL);
            result.ConvertedValue.Should().Be(25.6m);
            result.Warning.Should().BeNull();
            provider.CallCount.Should().Be(0);
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task UpdateEntryAsync_NullConvertedValue_EmptiesItWithoutWarning()
    {
        var entry = AddStoredEntry(_mae, Currency.BRL, 21.94m);

        var result = await CreateService().UpdateEntryAsync(_mae.Id, entry.Id, EntryUpdate(null));

        entry.ConvertedValue.Should().BeNull();
        result.Warning.Should().BeNull();
    }

    [Fact]
    public async Task UpdateEntryAsync_ConvertedValueRoundsToZero_SavesItEmptyWithWarning()
    {
        var entry = AddStoredEntry(_mae, Currency.GBP, 900m);

        var result = await CreateService().UpdateEntryAsync(_mae.Id, entry.Id, EntryUpdate(0.004m));

        entry.ConvertedValue.Should().BeNull();
        result.Warning.Should().Be("The BRL value rounds to 0.00, so it was left empty.");
    }

    [Fact]
    public async Task UpdateEntryAsync_FutureDate_RefusedAndNothingChanged()
    {
        var entry = AddStoredEntry(_mae, Currency.BRL, 21.94m);

        var act = () => CreateService().UpdateEntryAsync(_mae.Id, entry.Id, EntryUpdate(25.6m, date: TestClock.Today.AddDays(1)));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Date must not be in the future.");
        entry.Date.Should().Be(EntryDate);
        _repository.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateEntryAsync_InvalidValue_RefusedAndNothingChanged()
    {
        var entry = AddStoredEntry(_mae, Currency.BRL, 21.94m);

        var act = () => CreateService().UpdateEntryAsync(_mae.Id, entry.Id, EntryUpdate(-1m));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("The converted value must be greater than zero.");
        entry.Amount.Should().Be(150m);
        entry.Description.Should().Be("Farmácia");
        _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.SettlementService.UpdateEntry");
    }

    [Fact]
    public async Task UpdateEntryAsync_PaidByNotYouOrOther_Refused()
    {
        var entry = AddStoredEntry(_mae, Currency.BRL, 21.94m);

        var act = () => CreateService().UpdateEntryAsync(_mae.Id, entry.Id, EntryUpdate(25.6m, paidBy: "other"));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Paid by must be You or Other.");
    }

    [Fact]
    public async Task UpdateEntryAsync_EntryOfAnotherAccount_ThrowsKeyNotFound()
    {
        var entry = AddStoredEntry(_son, Currency.GBP, null);

        var act = () => CreateService().UpdateEntryAsync(_mae.Id, entry.Id, EntryUpdate(null));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateEntryAsync_InactiveAccount_Refused()
    {
        var entry = AddStoredEntry(_mae, Currency.BRL, 21.94m);
        _mae.SetActive(false);

        var act = () => CreateService().UpdateEntryAsync(_mae.Id, entry.Id, EntryUpdate(25.6m));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Mãe is inactive.");
    }

    [Fact]
    public async Task DeleteEntryAsync_EntryOfTheAccount_RemovesItAndSaves()
    {
        var entry = AddStoredEntry(_mae, Currency.BRL, 21.94m);

        await CreateService().DeleteEntryAsync(_mae.Id, entry.Id);

        _repository.SettlementTransactions.Should().BeEmpty();
        _repository.SaveChangesCallCount.Should().Be(1);
        _tracer.Spans.Should().ContainSingle(s => s.Name == "CashFlow.SettlementService.DeleteEntry")
            .Which.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
    }

    [Fact]
    public async Task DeleteEntryAsync_EntryOfAnotherAccount_ThrowsKeyNotFoundAndKeepsIt()
    {
        var entry = AddStoredEntry(_son, Currency.GBP, null);

        var act = () => CreateService().DeleteEntryAsync(_mae.Id, entry.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repository.SettlementTransactions.Should().ContainSingle();
        _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.SettlementService.DeleteEntry");
    }

    [Fact]
    public async Task DeleteEntryAsync_InactiveAccount_RefusedAndKeepsIt()
    {
        var entry = AddStoredEntry(_mae, Currency.BRL, 21.94m);
        _mae.SetActive(false);

        var act = () => CreateService().DeleteEntryAsync(_mae.Id, entry.Id);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Mãe is inactive.");
        _repository.SettlementTransactions.Should().ContainSingle();
    }
}
