using Financial.CashFlow.Application.Models;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class CreditCardCalendarSyncServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 7, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeCalendarProvider _provider;
    private readonly FakeCalendarConnectionStore _connectionStore;
    private readonly FakeCalendarIntegrationService _calendarIntegrationService;
    private readonly StubCashFlowRepository _repository;
    private readonly CardStatementService _cardStatementService;
    private readonly CreditCardCalendarSyncStatusStore _statusStore;
    private readonly FakeTimeProvider _timeProvider;
    private readonly CreditCardCalendarSyncService _sut;

    public CreditCardCalendarSyncServiceTests()
    {
        _provider = new FakeCalendarProvider();
        _connectionStore = new FakeCalendarConnectionStore();
        _calendarIntegrationService = new FakeCalendarIntegrationService();
        _repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        _cardStatementService = new CardStatementService(_repository, NullLogger<CardStatementService>.Instance, new RecordingTelemetryTracer());
        _statusStore = new CreditCardCalendarSyncStatusStore();
        _timeProvider = new FakeTimeProvider(Now);
        _sut = new CreditCardCalendarSyncService(
            _provider,
            _connectionStore,
            _calendarIntegrationService,
            _cardStatementService,
            _repository,
            _statusStore,
            new RecordingTelemetryTracer(),
            NullLogger<CreditCardCalendarSyncService>.Instance,
            _timeProvider);
    }

    private void Connect(string calendarId = "cal-1") =>
        _connectionStore.Save(new CalendarConnection(
            "user@gmail.com", calendarId, "access-token", "refresh-token", Now.AddHours(1), Now.AddDays(-1), new Dictionary<Guid, string>()));

    private CreditCard Card(string name) => _repository.CreditCards.First(c => c.Name == name);

    private static Expense AddChargeWithInvoiceDate(StubCashFlowRepository repository, DateOnly invoiceDate, decimal value, CreditCard card) =>
        AddCharge(repository, invoiceDate, value, card, invoiceDate);

    private static Expense AddCharge(StubCashFlowRepository repository, DateOnly date, decimal value, CreditCard card, DateOnly? invoiceDate = null)
    {
        var expense = Expense.Create(date, "Charge", value, Category.Create("Mercado"), null, card, invoiceDate);
        repository.Expenses.Add(expense);
        return expense;
    }

    private async Task<CreditCardCalendarSyncState?> WaitForResolvedStateAsync(Guid creditCardId, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(2));
        while (DateTime.UtcNow < deadline)
        {
            var status = _statusStore.GetStatus(creditCardId);
            if (status is not null && status.State != CreditCardCalendarSyncState.Pending)
            {
                return status.State;
            }

            await Task.Delay(10);
        }

        return _statusStore.GetStatus(creditCardId)?.State;
    }

    [Fact]
    public async Task ResyncAsync_WithUnknownCreditCardId_ThrowsKeyNotFoundException()
    {
        Connect();

        var act = async () => await _sut.ResyncAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ResyncAsync_WhenNotConnected_DoesNothing()
    {
        var card = Card("BarclaysPlatinumVisa8003");
        card.Update(card.Name, isActive: true, new DateOnly(2026, 9, 10));

        await _sut.ResyncAsync(card.Id);

        _provider.CreatedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task ResyncAsync_FirstSync_CreatesTheEvent_AndSyncStatusIsSynced()
    {
        Connect();
        var card = Card("BarclaysPlatinumVisa8003");
        card.Update(card.Name, isActive: true, new DateOnly(2026, 9, 10));
        AddChargeWithInvoiceDate(_repository, new DateOnly(2026, 9, 10), 123.45m, card);

        var result = await _sut.ResyncAsync(card.Id);

        result.State.Should().Be("Synced");
        _provider.CreatedEvents.Should().ContainSingle(e => e.CalendarId == "cal-1" && e.Date == new DateOnly(2026, 9, 10));
        _provider.CreatedEvents[0].Title.Should().Be($"{card.Name} — Due 123.45");
        _connectionStore.Load()!.CardEventIds.Should().ContainKey(card.Id).WhoseValue.Should().Be(_provider.CreatedEventId);
    }

    [Fact]
    public async Task ResyncAsync_SecondSync_UpdatesTheSameEventInstead_OfCreatingASecondOne()
    {
        Connect();
        var card = Card("BarclaysPlatinumVisa8003");
        card.Update(card.Name, isActive: true, new DateOnly(2026, 9, 10));

        await _sut.ResyncAsync(card.Id);
        card.Update(card.Name, isActive: true, new DateOnly(2026, 9, 15));
        await _sut.ResyncAsync(card.Id);

        _provider.CreatedEvents.Should().HaveCount(1);
        _provider.UpdatedEvents.Should().ContainSingle(e => e.EventId == _provider.CreatedEventId && e.Date == new DateOnly(2026, 9, 15));
    }

    [Fact]
    public async Task ResyncAsync_WithNoChargesPostedForThePeriod_ShowsZeroBalanceWithTheNote()
    {
        Connect();
        var card = Card("BarclaysPlatinumVisa8003");
        card.Update(card.Name, isActive: true, new DateOnly(2026, 9, 10));

        await _sut.ResyncAsync(card.Id);

        _provider.CreatedEvents[0].Title.Should().Be($"{card.Name} — Due 0.00");
        _provider.CreatedEvents[0].Description.Should().Contain("(no charges posted to this invoice yet)");
    }

    [Fact]
    public async Task ResyncAsync_WhenDueDateIsCleared_RemovesTheExistingEvent()
    {
        Connect();
        var card = Card("BarclaysPlatinumVisa8003");
        card.Update(card.Name, isActive: true, new DateOnly(2026, 9, 10));
        await _sut.ResyncAsync(card.Id);

        card.Update(card.Name, isActive: true, nextInvoiceDueDate: null);
        var result = await _sut.ResyncAsync(card.Id);

        result.State.Should().Be("Synced");
        _provider.DeletedEvents.Should().ContainSingle(e => e.EventId == _provider.CreatedEventId);
        _connectionStore.Load()!.CardEventIds.Should().NotContainKey(card.Id);
    }

    [Fact]
    public async Task ResyncAsync_WhenCardBecomesInactive_RemovesTheExistingEvent()
    {
        Connect();
        var card = Card("BarclaysPlatinumVisa8003");
        card.Update(card.Name, isActive: true, new DateOnly(2026, 9, 10));
        await _sut.ResyncAsync(card.Id);

        card.Update(card.Name, isActive: false, new DateOnly(2026, 9, 10));
        await _sut.ResyncAsync(card.Id);

        _provider.DeletedEvents.Should().ContainSingle(e => e.EventId == _provider.CreatedEventId);
    }

    [Fact]
    public async Task TriggerSync_WhenCardWasDeleted_RemovesItsEvent()
    {
        Connect();
        var card = Card("BarclaysPlatinumVisa8003");
        card.Update(card.Name, isActive: true, new DateOnly(2026, 9, 10));
        await _sut.ResyncAsync(card.Id);
        var cardId = card.Id;
        _repository.CreditCards.RemoveAll(c => c.Id == cardId);

        _sut.TriggerSync(cardId);
        await WaitForResolvedStateAsync(cardId);

        _provider.DeletedEvents.Should().ContainSingle(e => e.EventId == _provider.CreatedEventId);
    }

    [Fact]
    public async Task ResyncAsync_WhenTheProviderCallFails_MarksErrorStatus_AndLeavesTheExistingEventUntouched()
    {
        Connect();
        var card = Card("BarclaysPlatinumVisa8003");
        card.Update(card.Name, isActive: true, new DateOnly(2026, 9, 10));
        await _sut.ResyncAsync(card.Id);
        var originalEventId = _connectionStore.Load()!.CardEventIds[card.Id];

        _provider.UpdateEventThrows = true;
        card.Update(card.Name, isActive: true, new DateOnly(2026, 9, 20));
        var result = await _sut.ResyncAsync(card.Id);

        result.State.Should().Be("Error");
        result.LastError.Should().NotBeNullOrEmpty();
        _connectionStore.Load()!.CardEventIds[card.Id].Should().Be(originalEventId);
    }

    [Fact]
    public async Task TriggerSync_MarksPendingImmediately_ThenSyncedOnceTheBackgroundAttemptCompletes()
    {
        Connect();
        var card = Card("BarclaysPlatinumVisa8003");
        card.Update(card.Name, isActive: true, new DateOnly(2026, 9, 10));

        _sut.TriggerSync(card.Id);
        var immediateState = _statusStore.GetStatus(card.Id)?.State;
        var resolvedState = await WaitForResolvedStateAsync(card.Id);

        immediateState.Should().Be(CreditCardCalendarSyncState.Pending);
        resolvedState.Should().Be(CreditCardCalendarSyncState.Synced);
    }

    [Fact]
    public void TriggerSync_WhenNotConnected_DoesNothing()
    {
        var card = Card("BarclaysPlatinumVisa8003");
        card.Update(card.Name, isActive: true, new DateOnly(2026, 9, 10));

        _sut.TriggerSync(card.Id);

        _statusStore.GetStatus(card.Id).Should().BeNull();
        _provider.CreatedEvents.Should().BeEmpty();
    }

    [Fact]
    public void TriggerSync_WhenConnectionIsRevoked_DoesNothing()
    {
        _connectionStore.Save(new CalendarConnection(
            "user@gmail.com", "cal-1", "access-token", "refresh-token", Now.AddHours(1), Now.AddDays(-1), new Dictionary<Guid, string>())
        {
            RevokedReason = "token_revoked"
        });
        var card = Card("BarclaysPlatinumVisa8003");
        card.Update(card.Name, isActive: true, new DateOnly(2026, 9, 10));

        _sut.TriggerSync(card.Id);

        _statusStore.GetStatus(card.Id).Should().BeNull();
        _provider.CreatedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task ResyncAsync_WhenTheDedicatedCalendarWasDeleted_RecreatesItAndResyncsEveryQualifyingCard()
    {
        Connect("old-cal");
        var card1 = Card("BarclaysPlatinumVisa8003");
        var card2 = Card("BarclaysPlatinumVisa6007");
        card1.Update(card1.Name, isActive: true, new DateOnly(2026, 9, 10));
        card2.Update(card2.Name, isActive: true, new DateOnly(2026, 9, 12));
        await _sut.ResyncAsync(card1.Id);
        await _sut.ResyncAsync(card2.Id);

        _provider.NotFoundCalendarId = "old-cal";
        _provider.CreatedCalendarId = "new-cal";

        var result = await _sut.ResyncAsync(card1.Id);

        result.State.Should().Be("Synced");
        var connection = _connectionStore.Load()!;
        connection.CalendarId.Should().Be("new-cal");
        connection.CardEventIds.Should().ContainKeys(card1.Id, card2.Id);
        _provider.CreatedEvents.Should().Contain(e => e.CalendarId == "new-cal").And.HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task ResyncAllAsync_SyncsEveryActiveCardWithADueDate_AndSkipsTheRest()
    {
        Connect();
        var due = Card("BarclaysPlatinumVisa8003");
        due.Update(due.Name, isActive: true, new DateOnly(2026, 9, 10));
        var noDueDate = Card("BarclaysPlatinumVisa6007");
        noDueDate.Update(noDueDate.Name, isActive: true, nextInvoiceDueDate: null);

        var results = await _sut.ResyncAllAsync();

        results.Should().ContainSingle(r => r.CreditCardId == due.Id && r.State == "Synced");
        results.Should().NotContain(r => r.CreditCardId == noDueDate.Id);
        _provider.CreatedEvents.Should().ContainSingle();
    }

    [Fact]
    public void GetSyncStatuses_ReflectsWhatHasBeenTracked()
    {
        _statusStore.SetSynced(Guid.NewGuid(), Now);

        var statuses = _sut.GetSyncStatuses();

        statuses.Should().ContainSingle(s => s.State == "Synced");
    }

    [Fact]
    public void GetSyncStatuses_CardHasAPersistedEventButNoInMemoryStatus_IsReportedAsSynced()
    {
        // Simulates a fresh process (e.g. after restarting Financial.App): the in-memory status
        // store starts empty, but the card's event already exists from a previous process's sync.
        var card = Card("BaAmex");
        Connect();
        _connectionStore.Save(_connectionStore.Load()! with { CardEventIds = new Dictionary<Guid, string> { [card.Id] = "event-1" } });

        var statuses = _sut.GetSyncStatuses();

        statuses.Should().ContainSingle(s => s.CreditCardId == card.Id && s.State == "Synced");
    }

    [Fact]
    public void GetSyncStatuses_InMemoryStatusTakesPrecedenceOverThePersistedEventMapping()
    {
        var card = Card("BaAmex");
        Connect();
        _connectionStore.Save(_connectionStore.Load()! with { CardEventIds = new Dictionary<Guid, string> { [card.Id] = "event-1" } });
        _statusStore.SetError(card.Id, "Rate limit exceeded");

        var statuses = _sut.GetSyncStatuses();

        statuses.Should().ContainSingle(s => s.CreditCardId == card.Id && s.State == "Error");
    }
}
