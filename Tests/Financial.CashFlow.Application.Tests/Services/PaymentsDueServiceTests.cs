using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.TestUtilities;
using Microsoft.Extensions.Time.Testing;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

[Trait("Category", "Unit")]
public class PaymentsDueServiceTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger<PaymentsDueService> Logger = NullLogger<PaymentsDueService>.Instance;

    // Pinned to a fixed UTC instant with TimeSpan.Zero, and tests use TimeZoneInfo.Utc, so "today" is
    // deterministic regardless of the machine running the suite - the same guarantee production gets
    // from TimeZoneInfo.Local, just pinned instead of host-dependent.
    private static readonly DateTimeOffset PinnedNow = new(2026, 6, 15, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly PinnedToday = DateOnly.FromDateTime(PinnedNow.UtcDateTime);

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly PaymentsDueService _sut;

    public PaymentsDueServiceTests()
    {
        _repository = new StubCashFlowRepository();
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    private PaymentsDueService CreateService(
        StubCashFlowRepository? repository = null, TimeProvider? timeProvider = null) =>
        new(
            repository ?? _repository,
            _tracer,
            Logger,
            timeProvider ?? new FakeTimeProvider(PinnedNow),
            TimeZoneInfo.Utc);

    private static RecurringBill CreateBill(int dueDay, string description = "Internet") =>
        RecurringBill.Create(dueDay, description, 50m, Area.UK, string.Empty, null, null);

    private Account CreateCard(string name, DateOnly? nextInvoiceDueDate)
    {
        var card = TestAccounts.CreditCard(name);
        _repository.SetCardDueDate(card, nextInvoiceDueDate);
        return card;
    }

    private void AddStatement(Account card, DateOnly period, bool paid = false)
    {
        var statement = _repository.CardStatements.FirstOrDefault(existing => existing.IsFor(card.Id, period.Year, period.Month));
        if (statement is null)
        {
            statement = CardStatement.Create(card, period.Year, period.Month);
            _repository.AddCardStatement(statement);
        }

        if (paid)
        {
            statement.MarkLegacyPaid();
        }
    }

    [Fact]
    public void GetPaymentsDue_MensaisWithStatusUnsetAndDueDayInWindow_IsIncluded()
    {
        _repository.AddRecurringBill(CreateBill(PinnedToday.Day, "Internet"));

        var result = _sut.GetPaymentsDue();

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Type = "Mensais",
            Name = "Internet",
            DueDate = PinnedToday,
            DaysRemaining = 0
        });
    }

    [Theory]
    [InlineData(BillStatus.Scheduled)]
    [InlineData(BillStatus.Paid)]
    public void GetPaymentsDue_MensaisWithStatusScheduledOrPaid_IsExcluded(BillStatus status)
    {
        var bill = CreateBill(PinnedToday.Day);
        bill.SetStatus(status);
        _repository.AddRecurringBill(bill);

        var result = _sut.GetPaymentsDue();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetPaymentsDue_MensaisDueDayBeyondMonthLength_ClampsToLastDayOfMonth()
    {
        var pinnedFebruary = new DateTimeOffset(2027, 2, 24, 0, 0, 0, TimeSpan.Zero);
        var service = CreateService(timeProvider: new FakeTimeProvider(pinnedFebruary));
        _repository.AddRecurringBill(CreateBill(31, "Rent"));

        var result = service.GetPaymentsDue();

        result.Should().ContainSingle().Which.DueDate.Should().Be(new DateOnly(2027, 2, 28));
    }

    [Fact]
    public void GetPaymentsDue_On29th_ExcludesABillWhoseDueDayAlreadyPassedThisMonth()
    {
        var twentyNinth = new DateTimeOffset(2026, 6, 29, 0, 0, 0, TimeSpan.Zero);
        var service = CreateService(timeProvider: new FakeTimeProvider(twentyNinth));
        _repository.AddRecurringBill(CreateBill(2, "Early Bill"));
        _repository.AddRecurringBill(CreateBill(30, "Month End Bill"));

        var result = service.GetPaymentsDue();

        result.Should().ContainSingle().Which.Name.Should().Be("Month End Bill");
    }

    [Fact]
    [Trait("AC", "P60-F07-credit-card-template-and-card-migration-10")]
    public void GetPaymentsDue_CreditCardWithNextInvoiceDueDateInWindow_IsIncluded()
    {
        _repository.AddAccount(CreateCard("Nubank", PinnedToday.AddDays(3)));

        var result = _sut.GetPaymentsDue();

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Type = "CreditCard",
            Name = "Nubank",
            DueDate = PinnedToday.AddDays(3),
            DaysRemaining = 3
        });
    }

    [Fact]
    [Trait("AC", "P60-F07-credit-card-template-and-card-migration-10")]
    public void GetPaymentsDue_CreditCardWithPaidStatementForInvoicePeriod_IsExcluded()
    {
        var dueDate = PinnedToday.AddDays(3);
        var card = CreateCard("Nubank", dueDate);
        _repository.AddAccount(card);
        AddStatement(card, dueDate, paid: true);

        var result = _sut.GetPaymentsDue();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetPaymentsDue_CreditCardWithUnpaidStatementForInvoicePeriod_IsIncluded()
    {
        var dueDate = PinnedToday.AddDays(3);
        var card = CreateCard("Nubank", dueDate);
        _repository.AddAccount(card);
        AddStatement(card, dueDate);

        var result = _sut.GetPaymentsDue();

        result.Should().ContainSingle().Which.Name.Should().Be("Nubank");
    }

    [Fact]
    public void GetPaymentsDue_CreditCardWithPaidStatementForDifferentPeriod_IsIncluded()
    {
        var dueDate = PinnedToday.AddDays(3);
        var card = CreateCard("Nubank", dueDate);
        _repository.AddAccount(card);
        AddStatement(card, dueDate.AddMonths(-1), paid: true);

        var result = _sut.GetPaymentsDue();

        result.Should().ContainSingle().Which.Name.Should().Be("Nubank");
    }

    [Fact]
    public void GetPaymentsDue_CreditCardWithNullNextInvoiceDueDate_IsExcluded()
    {
        _repository.AddAccount(CreateCard("Nubank", nextInvoiceDueDate: null));

        var result = _sut.GetPaymentsDue();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetPaymentsDue_DueDateEqualsToday_DaysRemainingIsZero()
    {
        _repository.AddAccount(CreateCard("Nubank", PinnedToday));

        var result = _sut.GetPaymentsDue();

        result.Should().ContainSingle().Which.DaysRemaining.Should().Be(0);
    }

    [Fact]
    public void GetPaymentsDue_DueDateFiveDaysOut_IsIncludedWithDaysRemainingFive()
    {
        _repository.AddAccount(CreateCard("Nubank", PinnedToday.AddDays(5)));

        var result = _sut.GetPaymentsDue();

        result.Should().ContainSingle().Which.DaysRemaining.Should().Be(5);
    }

    [Fact]
    public void GetPaymentsDue_DueDateSixDaysOut_IsExcluded()
    {
        _repository.AddAccount(CreateCard("Nubank", PinnedToday.AddDays(6)));

        var result = _sut.GetPaymentsDue();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetPaymentsDue_DueDateInPast_IsExcluded()
    {
        _repository.AddAccount(CreateCard("Nubank", PinnedToday.AddDays(-1)));

        var result = _sut.GetPaymentsDue();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetPaymentsDue_MultipleQualifyingPayments_SortedByDueDateAscending()
    {
        _repository.AddAccount(CreateCard("Third", PinnedToday.AddDays(4)));
        _repository.AddAccount(CreateCard("First", PinnedToday));
        _repository.AddAccount(CreateCard("Second", PinnedToday.AddDays(2)));

        var result = _sut.GetPaymentsDue();

        result.Select(p => p.Name).Should().Equal("First", "Second", "Third");
    }

    [Fact]
    public void GetPaymentsDue_SameDueDate_MensaisSortsBeforeCreditCard()
    {
        var sharedDueDay = PinnedToday.AddDays(2).Day;
        _repository.AddAccount(CreateCard("Nubank", PinnedToday.AddDays(2)));
        _repository.AddRecurringBill(CreateBill(sharedDueDay, "Internet"));

        var result = _sut.GetPaymentsDue();

        result.Select(p => p.Type).Should().Equal("Mensais", "CreditCard");
    }

    [Fact]
    public void GetPaymentsDue_SameDueDateAndType_SortedByNameAscending()
    {
        _repository.AddAccount(CreateCard("Beta", PinnedToday.AddDays(1)));
        _repository.AddAccount(CreateCard("Alpha", PinnedToday.AddDays(1)));

        var result = _sut.GetPaymentsDue();

        result.Select(p => p.Name).Should().Equal("Alpha", "Beta");
    }

    [Fact]
    public void GetPaymentsDue_SameDueDateAndType_NameSortIsCaseInsensitive()
    {
        _repository.AddRecurringBill(CreateBill(PinnedToday.Day, "Thames Water"));
        _repository.AddRecurringBill(CreateBill(PinnedToday.Day, "eon"));

        var result = _sut.GetPaymentsDue();

        result.Select(p => p.Name).Should().Equal("eon", "Thames Water");
    }

    [Fact]
    public void GetPaymentsDue_RecurringBillRepositoryThrows_ReturnsCreditCardsOnlyAndLogsError()
    {
        _repository.AddAccount(CreateCard("Nubank", PinnedToday));
        _repository.ThrowOnNextGetRecurringBills = true;

        var result = _sut.GetPaymentsDue();

        result.Should().ContainSingle().Which.Name.Should().Be("Nubank");
    }

    [Fact]
    public void GetPaymentsDue_CreditCardRepositoryThrows_ReturnsMensaisOnlyAndLogsError()
    {
        _repository.AddRecurringBill(CreateBill(PinnedToday.Day, "Internet"));
        _repository.ThrowOnNextRead = new InvalidOperationException("Simulated read failure.");

        var result = _sut.GetPaymentsDue();

        result.Should().ContainSingle().Which.Name.Should().Be("Internet");
    }

    [Fact]
    public void GetPaymentsDue_UsesInjectedTimeProvider_ForTodayComputation()
    {
        _repository.AddRecurringBill(CreateBill(20, "Internet"));

        var early = CreateService(timeProvider: new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero)));
        var late = CreateService(timeProvider: new FakeTimeProvider(new DateTimeOffset(2026, 6, 25, 0, 0, 0, TimeSpan.Zero)));

        early.GetPaymentsDue().Should().ContainSingle(p => p.Name == "Internet");
        late.GetPaymentsDue().Should().BeEmpty();
    }

    [Fact]
    public void GetPaymentsDue_NoQualifyingPayments_ReturnsEmptyArray()
    {
        var result = _sut.GetPaymentsDue();

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetPaymentsDue_TimeProviderThrowsUnexpectedly_ReturnsEmptyArray()
    {
        var sut = CreateService(timeProvider: new ThrowingTimeProvider());

        var result = sut.GetPaymentsDue();

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    private sealed class ThrowingTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => throw new InvalidOperationException("simulated failure");
    }
}
