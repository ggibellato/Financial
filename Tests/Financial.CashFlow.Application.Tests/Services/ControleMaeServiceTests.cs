using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

[Trait("Category", "Unit")]
public class ControleMaeServiceTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger<ControleMaeService> Logger = NullLogger<ControleMaeService>.Instance;
    private const decimal DefaultRate = 1.5m;

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly ControleMaeService _sut;
    private readonly Account _mae = TestAccounts.Settlement("Mãe", [Currency.BRL, Currency.GBP], "Mãe");

    public ControleMaeServiceTests()
    {
        _repository = new StubCashFlowRepository();
        _repository.Accounts.Add(_mae);
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    private ControleMaeService CreateService(StubCashFlowRepository? repository = null, IExchangeRateProvider? exchangeRateProvider = null) =>
        new(repository ?? _repository, exchangeRateProvider ?? new StubExchangeRateProvider(DefaultRate), TestClock.At(), _tracer, Logger);

    [Fact]
    [Trait("AC", "P49-F01-shared-exchange-rate-provider-04")]
    public async Task CreateEntryAsync_WithSuccessfulRateLookup_PopulatesBothCurrenciesAndSaves()
    {
        var provider = new StubExchangeRateProvider(0.146m);
        var service = CreateService(exchangeRateProvider: provider);

        var result = await service.CreateEntryAsync(new MaeLedgerEntryCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "School supplies",
            Note = "Term start",
            SourceCurrency = "BRL",
            SourceValue = 350m
        });

        using (new AssertionScope())
        {
            result.BrlValue.Should().Be(350m);
            result.GbpValue.Should().Be(51.1m);
            _repository.SettlementTransactions.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task CreateEntryAsync_AmountAndRateWithManyDecimals_SavesBothRoundedToTwo()
    {
        var service = CreateService(exchangeRateProvider: new StubExchangeRateProvider(100.0012345m));

        var result = await service.CreateEntryAsync(NewEntry("BRL", -10.005m));

        using (new AssertionScope())
        {
            var stored = _repository.SettlementTransactions.Single();
            stored.Amount.Should().Be(10.01m);
            stored.ConvertedValue.Should().Be(1001.01m);
            result.BrlValue.Should().Be(-10.01m);
            result.GbpValue.Should().Be(-1001.01m);
        }
    }

    [Fact]
    public async Task CreateEntryAsync_WithFailedRateLookup_StillSavesWithOnlyEnteredCurrency()
    {
        var provider = new StubExchangeRateProvider(null);
        var service = CreateService(exchangeRateProvider: provider);

        var result = await service.CreateEntryAsync(new MaeLedgerEntryCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "Medical appointment",
            SourceCurrency = "GBP",
            SourceValue = 40m
        });

        using (new AssertionScope())
        {
            result.GbpValue.Should().Be(40m);
            result.BrlValue.Should().BeNull();
            _repository.SettlementTransactions.Should().ContainSingle();
        }
    }

    [Fact]
    public async Task CreateEntryAsync_WithFutureDate_ThrowsBeforeTouchingRepositoryOrProvider()
    {
        var provider = new StubExchangeRateProvider(DefaultRate);
        var service = CreateService(exchangeRateProvider: provider);
        var futureDate = TestClock.Today.AddDays(1);

        var act = async () => await service.CreateEntryAsync(new MaeLedgerEntryCreateDTO
        {
            Date = futureDate,
            Description = "Future entry",
            SourceCurrency = "BRL",
            SourceValue = 100m
        });

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>();
            _repository.SettlementTransactions.Should().BeEmpty();
            provider.CallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task CreateEntryAsync_WithBlankDescription_Throws()
    {
        var act = async () => await _sut.CreateEntryAsync(new MaeLedgerEntryCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "   ",
            SourceCurrency = "BRL",
            SourceValue = 100m
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateEntryAsync_WithUnrecognizedCurrency_Throws()
    {
        var act = async () => await _sut.CreateEntryAsync(new MaeLedgerEntryCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "Test",
            SourceCurrency = "USD",
            SourceValue = 100m
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateEntryAsync_WithZeroValue_Throws()
    {
        var act = async () => await _sut.CreateEntryAsync(new MaeLedgerEntryCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "Test",
            SourceCurrency = "BRL",
            SourceValue = 0m
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateEntryAsync_PositiveValue_IsPaidByYouWithPositiveAmountAndConvertedValueInTheOtherCurrency()
    {
        await _sut.CreateEntryAsync(NewEntry("GBP", 80m));

        var entry = _repository.SettlementTransactions.Single();
        using (new AssertionScope())
        {
            entry.Account.Should().BeSameAs(_mae);
            entry.PaidBy.Should().Be(SettlementPayer.You);
            entry.Currency.Should().Be(Currency.GBP);
            entry.Amount.Should().Be(80m);
            entry.ConvertedValue.Should().Be(80m * DefaultRate);
        }
    }

    [Fact]
    public async Task CreateEntryAsync_NegativeValue_IsPaidByOtherWithPositiveAmountAndSignedValuesInTheResult()
    {
        var result = await _sut.CreateEntryAsync(NewEntry("BRL", -200m));

        var entry = _repository.SettlementTransactions.Single();
        using (new AssertionScope())
        {
            entry.PaidBy.Should().Be(SettlementPayer.Other);
            entry.Amount.Should().Be(200m);
            entry.ConvertedValue.Should().Be(300m);
            result.SourceCurrency.Should().Be("BRL");
            result.BrlValue.Should().Be(-200m);
            result.GbpValue.Should().Be(-300m);
        }
    }

    [Fact]
    public async Task CreateEntryAsync_OneCurrencyAccount_StoresNoConvertedValueAndAsksNoRate()
    {
        var provider = new StubExchangeRateProvider(DefaultRate);
        var repository = new StubCashFlowRepository();
        repository.Accounts.Add(TestAccounts.Settlement("Samuel", [Currency.GBP], "Samuel"));

        await CreateService(repository, provider).CreateEntryAsync(NewEntry("GBP", 15m));

        repository.SettlementTransactions.Single().ConvertedValue.Should().BeNull();
        provider.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateEntryAsync_NoSettlementAccount_Returns400Message()
    {
        var repository = new StubCashFlowRepository();
        repository.Accounts.Add(TestAccounts.Bank("Barclays"));

        var act = async () => await CreateService(repository).CreateEntryAsync(NewEntry("BRL", 10m));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Create a Settlement account first.");
        repository.SettlementTransactions.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateEntryAsync_Success_RecordsSuccessfulSpan()
    {
        await _sut.CreateEntryAsync(NewEntry("BRL", 10m));

        _tracer.Spans.Should().ContainSingle(s => s.Name == "CashFlow.ControleMaeService.CreateEntry")
            .Which.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
    }

    [Fact]
    public async Task CreateEntryAsync_Invalid_RecordsFailedSpan()
    {
        var act = async () => await _sut.CreateEntryAsync(NewEntry("BRL", 0m));

        await act.Should().ThrowAsync<ArgumentException>();
        _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ControleMaeService.CreateEntry");
    }

    [Fact]
    public void GetEntriesFromDate_ReturnsOnlyEntriesOnOrAfterDate()
    {
        AddEntry(new DateOnly(2026, 6, 30), "Before", SettlementPayer.You, Currency.BRL, 10m, 1m);
        AddEntry(new DateOnly(2026, 7, 1), "OnDate", SettlementPayer.You, Currency.BRL, 10m, 1m);
        AddEntry(new DateOnly(2026, 8, 10), "After", SettlementPayer.You, Currency.BRL, 10m, 1m);

        var result = _sut.GetEntriesFromDate(new DateOnly(2026, 7, 1));

        result.Should().HaveCount(2);
        result.Select(e => e.Description).Should().ContainInOrder("OnDate", "After");
    }

    [Fact]
    public void GetEntriesFromDate_ValuesAreTheSignedValuesAndAnEmptyConvertedValueIsNull()
    {
        AddEntry(new DateOnly(2026, 7, 1), "Paid by Mãe", SettlementPayer.Other, Currency.GBP, 80m, 491.04m);
        AddEntry(new DateOnly(2026, 7, 2), "No rate", SettlementPayer.You, Currency.BRL, 100m, null);

        var result = _sut.GetEntriesFromDate(new DateOnly(2026, 1, 1));

        using (new AssertionScope())
        {
            result[0].SourceCurrency.Should().Be("GBP");
            result[0].GbpValue.Should().Be(-80m);
            result[0].BrlValue.Should().Be(-491.04m);
            result[1].BrlValue.Should().Be(100m);
            result[1].GbpValue.Should().BeNull();
        }
    }

    [Fact]
    public void GetEntriesFromDate_EntriesOfAnotherSettlementAccount_AreLeftOut()
    {
        var son = TestAccounts.Settlement("Samuel", [Currency.GBP], "Samuel");
        _repository.Accounts.Add(son);
        _repository.SettlementTransactions.Add(SettlementTransaction.Create(son, new DateOnly(2026, 7, 1), "Son", null, SettlementPayer.You, Currency.GBP, 10m, null));
        AddEntry(new DateOnly(2026, 7, 1), "Mãe", SettlementPayer.You, Currency.BRL, 10m, null);

        _sut.GetEntriesFromDate(new DateOnly(2026, 1, 1)).Select(e => e.Description).Should().Equal("Mãe");
    }

    [Fact]
    public void GetTotals_SumsSignedValuesPerCurrencyAcrossAllEntriesRegardlessOfDate()
    {
        AddEntry(new DateOnly(2020, 1, 1), "Old", SettlementPayer.You, Currency.BRL, 100m, 10m);
        AddEntry(new DateOnly(2026, 7, 10), "Recent", SettlementPayer.Other, Currency.GBP, 5m, null);

        var result = _sut.GetTotals();

        result.TotalBrlValue.Should().Be(100m);
        result.TotalGbpValue.Should().Be(5m);
    }

    [Fact]
    public void GetTotals_MaeBalancePerCurrency_LeavingOtherAccountsEntriesOut()
    {
        var son = TestAccounts.Settlement("Samuel", [Currency.GBP], "Samuel");
        _repository.Accounts.Add(son);
        _repository.SettlementTransactions.Add(SettlementTransaction.Create(son, new DateOnly(2026, 7, 1), "Son", null, SettlementPayer.You, Currency.GBP, 70m, null));
        AddEntry(new DateOnly(2022, 11, 25), "Pounds", SettlementPayer.You, Currency.GBP, 100m, 654.24m);
        AddEntry(new DateOnly(2026, 7, 10), "Paid by Mãe", SettlementPayer.Other, Currency.BRL, 486.36m, 72.06m);

        var result = _sut.GetTotals();

        using (new AssertionScope())
        {
            result.TotalBrlValue.Should().Be(167.88m);
            result.TotalGbpValue.Should().Be(27.94m);
        }
    }

    [Fact]
    public void GetTotals_NoSettlementAccount_IsZero()
    {
        var result = CreateService(new StubCashFlowRepository()).GetTotals();

        result.TotalBrlValue.Should().Be(0m);
        result.TotalGbpValue.Should().Be(0m);
    }

    [Theory]
    [InlineData(320.50, 40, SettlementPayer.You)]
    [InlineData(-320.50, -40, SettlementPayer.Other)]
    public async Task UpdateEntryValuesAsync_SignOfThePaidCurrencyValueSetsPaidBy(decimal brl, decimal gbp, SettlementPayer expected)
    {
        var entry = AddEntry(new DateOnly(2026, 7, 1), "Medical appointment", SettlementPayer.Other, Currency.GBP, 30m, null, "Note");

        var result = await _sut.UpdateEntryValuesAsync(entry.Id, new MaeLedgerEntryValuesUpdateDTO { BrlValue = brl, GbpValue = gbp });

        using (new AssertionScope())
        {
            entry.PaidBy.Should().Be(expected);
            entry.Amount.Should().Be(40m);
            entry.ConvertedValue.Should().Be(320.50m);
            result.BrlValue.Should().Be(brl);
            result.GbpValue.Should().Be(gbp);
            entry.Date.Should().Be(new DateOnly(2026, 7, 1));
            entry.Description.Should().Be("Medical appointment");
            entry.Note.Should().Be("Note");
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task UpdateEntryValuesAsync_ValuesWithFourDecimals_SavesThemWithTwo()
    {
        var entry = AddEntry(new DateOnly(2026, 7, 1), "Entry", SettlementPayer.You, Currency.BRL, 100m, 15m);

        var result = await _sut.UpdateEntryValuesAsync(entry.Id, new MaeLedgerEntryValuesUpdateDTO { BrlValue = -167.8768m, GbpValue = -7.9421m });

        using (new AssertionScope())
        {
            entry.Amount.Should().Be(167.88m);
            entry.ConvertedValue.Should().Be(7.94m);
            result.BrlValue.Should().Be(-167.88m);
            result.GbpValue.Should().Be(-7.94m);
        }
    }

    [Fact]
    public async Task UpdateEntryValuesAsync_MissingOtherValue_EmptiesTheConvertedValue()
    {
        var entry = AddEntry(new DateOnly(2026, 7, 1), "Entry", SettlementPayer.You, Currency.BRL, 100m, 15m);

        var result = await _sut.UpdateEntryValuesAsync(entry.Id, new MaeLedgerEntryValuesUpdateDTO { BrlValue = 120m });

        entry.ConvertedValue.Should().BeNull();
        result.GbpValue.Should().BeNull();
    }

    [Theory]
    [InlineData(null, 40, "BRL value is required and must not be zero.")]
    [InlineData(0, 40, "BRL value is required and must not be zero.")]
    [InlineData(100, -40, "BRL and GBP values must both be positive or both be negative.")]
    [InlineData(-100, 40, "BRL and GBP values must both be positive or both be negative.")]
    [InlineData(100, 0, "BRL and GBP values must both be positive or both be negative.")]
    public async Task UpdateEntryValuesAsync_MissingZeroOrOppositeSignedValues_RefusedAndNothingSaved(int? brl, int? gbp, string message)
    {
        var entry = AddEntry(new DateOnly(2026, 7, 1), "Entry", SettlementPayer.You, Currency.BRL, 100m, 15m);

        var act = async () => await _sut.UpdateEntryValuesAsync(entry.Id, new MaeLedgerEntryValuesUpdateDTO { BrlValue = (decimal?)brl, GbpValue = (decimal?)gbp });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage(message);
        entry.Amount.Should().Be(100m);
        entry.ConvertedValue.Should().Be(15m);
        _repository.SaveChangesCallCount.Should().Be(0);
        _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ControleMaeService.UpdateEntryValues");
    }

    [Fact]
    public async Task UpdateEntryValuesAsync_GbpEntryWithoutGbpValue_NamesGbp()
    {
        var entry = AddEntry(new DateOnly(2026, 7, 1), "Entry", SettlementPayer.You, Currency.GBP, 10m, null);

        var act = async () => await _sut.UpdateEntryValuesAsync(entry.Id, new MaeLedgerEntryValuesUpdateDTO { BrlValue = 60m });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("GBP value is required and must not be zero.");
    }

    [Fact]
    public async Task UpdateEntryValuesAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.UpdateEntryValuesAsync(Guid.NewGuid(), new MaeLedgerEntryValuesUpdateDTO
        {
            BrlValue = 10m,
            GbpValue = 1m
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateEntryValuesAsync_EntryOfAnotherAccount_ThrowsKeyNotFoundException()
    {
        var son = TestAccounts.Settlement("Samuel", [Currency.GBP], "Samuel");
        _repository.Accounts.Add(son);
        var entry = SettlementTransaction.Create(son, new DateOnly(2026, 7, 1), "Son", null, SettlementPayer.You, Currency.GBP, 10m, null);
        _repository.SettlementTransactions.Add(entry);

        var act = async () => await _sut.UpdateEntryValuesAsync(entry.Id, new MaeLedgerEntryValuesUpdateDTO { GbpValue = 12m });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteEntryAsync_ExistingId_RemovesEntryAndSaves()
    {
        var entry = AddEntry(new DateOnly(2026, 7, 1), "School supplies", SettlementPayer.You, Currency.BRL, 350m, 51.1m);

        await _sut.DeleteEntryAsync(entry.Id);

        _repository.SettlementTransactions.Should().BeEmpty();
        _repository.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteEntryAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.DeleteEntryAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteEntryAsync_EntryOfAnotherAccount_ThrowsKeyNotFoundAndKeepsIt()
    {
        var son = TestAccounts.Settlement("Samuel", [Currency.GBP], "Samuel");
        _repository.Accounts.Add(son);
        var entry = SettlementTransaction.Create(son, new DateOnly(2026, 7, 1), "Son", null, SettlementPayer.You, Currency.GBP, 10m, null);
        _repository.SettlementTransactions.Add(entry);

        var act = async () => await _sut.DeleteEntryAsync(entry.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repository.SettlementTransactions.Should().ContainSingle();
    }

    private static MaeLedgerEntryCreateDTO NewEntry(string currency, decimal value) => new()
    {
        Date = new DateOnly(2026, 7, 1),
        Description = "Entry",
        SourceCurrency = currency,
        SourceValue = value
    };

    private SettlementTransaction AddEntry(
        DateOnly date, string description, SettlementPayer paidBy, Currency currency, decimal amount, decimal? convertedValue, string note = "")
    {
        var entry = SettlementTransaction.Create(_mae, date, description, note, paidBy, currency, amount, convertedValue);
        _repository.SettlementTransactions.Add(entry);
        return entry;
    }

    [Fact]
    public void GetEntriesFromDate_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnNextRead = new InvalidOperationException("simulated failure");

        Action act = () => _sut.GetEntriesFromDate(new DateOnly(2026, 1, 1));

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.ControleMaeService.GetEntriesFromDate");
    }

    [Fact]
    public void GetTotals_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnNextRead = new InvalidOperationException("simulated failure");

        Action act = () => _sut.GetTotals();

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.ControleMaeService.GetTotals");
    }
}
