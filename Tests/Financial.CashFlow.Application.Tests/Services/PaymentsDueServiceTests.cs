using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

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

    private static CreditCard CreateCard(string name, DateOnly? nextInvoiceDueDate)
    {
        var card = CreditCard.Create(name);
        card.Update(name, isActive: true, nextInvoiceDueDate);
        return card;
    }

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new PaymentsDueService(null!, _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new PaymentsDueService(_repository, null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new PaymentsDueService(_repository, _tracer, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
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
    public void GetPaymentsDue_CreditCardWithNextInvoiceDueDateInWindow_IsIncluded()
    {
        _repository.AddCreditCard(CreateCard("Nubank", PinnedToday.AddDays(3)));

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
    public void GetPaymentsDue_CreditCardWithNullNextInvoiceDueDate_IsExcluded()
    {
        _repository.AddCreditCard(CreateCard("Nubank", nextInvoiceDueDate: null));

        var result = _sut.GetPaymentsDue();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetPaymentsDue_DueDateEqualsToday_DaysRemainingIsZero()
    {
        _repository.AddCreditCard(CreateCard("Nubank", PinnedToday));

        var result = _sut.GetPaymentsDue();

        result.Should().ContainSingle().Which.DaysRemaining.Should().Be(0);
    }

    [Fact]
    public void GetPaymentsDue_DueDateFiveDaysOut_IsIncludedWithDaysRemainingFive()
    {
        _repository.AddCreditCard(CreateCard("Nubank", PinnedToday.AddDays(5)));

        var result = _sut.GetPaymentsDue();

        result.Should().ContainSingle().Which.DaysRemaining.Should().Be(5);
    }

    [Fact]
    public void GetPaymentsDue_DueDateSixDaysOut_IsExcluded()
    {
        _repository.AddCreditCard(CreateCard("Nubank", PinnedToday.AddDays(6)));

        var result = _sut.GetPaymentsDue();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetPaymentsDue_DueDateInPast_IsExcluded()
    {
        _repository.AddCreditCard(CreateCard("Nubank", PinnedToday.AddDays(-1)));

        var result = _sut.GetPaymentsDue();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetPaymentsDue_MultipleQualifyingPayments_SortedByDueDateAscending()
    {
        _repository.AddCreditCard(CreateCard("Third", PinnedToday.AddDays(4)));
        _repository.AddCreditCard(CreateCard("First", PinnedToday));
        _repository.AddCreditCard(CreateCard("Second", PinnedToday.AddDays(2)));

        var result = _sut.GetPaymentsDue();

        result.Select(p => p.Name).Should().Equal("First", "Second", "Third");
    }

    [Fact]
    public void GetPaymentsDue_SameDueDate_MensaisSortsBeforeCreditCard()
    {
        var sharedDueDay = PinnedToday.AddDays(2).Day;
        _repository.AddCreditCard(CreateCard("Nubank", PinnedToday.AddDays(2)));
        _repository.AddRecurringBill(CreateBill(sharedDueDay, "Internet"));

        var result = _sut.GetPaymentsDue();

        result.Select(p => p.Type).Should().Equal("Mensais", "CreditCard");
    }

    [Fact]
    public void GetPaymentsDue_SameDueDateAndType_SortedByNameAscending()
    {
        _repository.AddCreditCard(CreateCard("Beta", PinnedToday.AddDays(1)));
        _repository.AddCreditCard(CreateCard("Alpha", PinnedToday.AddDays(1)));

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
        _repository.AddCreditCard(CreateCard("Nubank", PinnedToday));
        _repository.ThrowOnNextGetRecurringBills = true;

        var result = _sut.GetPaymentsDue();

        result.Should().ContainSingle().Which.Name.Should().Be("Nubank");
    }

    [Fact]
    public void GetPaymentsDue_CreditCardRepositoryThrows_ReturnsMensaisOnlyAndLogsError()
    {
        _repository.AddRecurringBill(CreateBill(PinnedToday.Day, "Internet"));
        _repository.ThrowOnNextGetCreditCards = true;

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
}
