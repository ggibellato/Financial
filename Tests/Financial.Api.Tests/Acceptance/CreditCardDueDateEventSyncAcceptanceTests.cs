using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Models;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Api.Tests.Acceptance;

public class CreditCardDueDateEventSyncAcceptanceTests : ApiEndpointTests
{
    private const string CalendarRoute = "/api/v1/financial/integrations/calendar";
    private const string CreditCardsRoute = "/api/v1/financial/credit-cards";
    private const string ExpensesRoute = "/api/v1/financial/expenses";
    private const string MercadoCategoryId = "8f3b1c1a-2e3a-4b1a-9a7f-600000000008";
    private static readonly DateTimeOffset Now = new(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeCalendarProvider _provider;

    public CreditCardDueDateEventSyncAcceptanceTests() : this(new FakeCalendarProvider())
    {
    }

    private CreditCardDueDateEventSyncAcceptanceTests(FakeCalendarProvider provider)
        : base(timeProvider: new FakeTimeProvider(Now), calendarProvider: provider)
    {
        _provider = provider;
    }

    private void Connect(string calendarId = "cal-1")
    {
        var store = Services.GetRequiredService<ICalendarConnectionStore>();
        store.Save(new CalendarConnection(
            "user@gmail.com", calendarId, "access-token", "refresh-token", Now.AddHours(1), Now.AddDays(-1), new Dictionary<Guid, string>()));
    }

    private async Task<CreditCardCalendarSyncStatusDTO?> WaitForResolvedSyncStatusAsync(Guid cardId, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        CreditCardCalendarSyncStatusDTO? status = null;
        while (DateTime.UtcNow < deadline)
        {
            var statuses = await Client.GetFromJsonAsync<List<CreditCardCalendarSyncStatusDTO>>($"{CalendarRoute}/credit-cards/sync-status");
            status = statuses?.FirstOrDefault(s => s.CreditCardId == cardId);
            if (status is not null && status.State != "Pending")
            {
                return status;
            }

            await Task.Delay(25);
        }

        return status;
    }

    private async Task SetDueDateAsync(Guid cardId, string name, bool isActive, DateOnly? dueDate)
    {
        var response = await Client.PutAsJsonAsync($"{CreditCardsRoute}/{cardId}",
            new CreditCardUpdateDTO { Name = name, IsActive = isActive, NextInvoiceDueDate = dueDate });
        response.EnsureSuccessStatusCode();
    }

    private async Task AddChargeAsync(Guid cardId, DateOnly invoiceDate, decimal value)
    {
        var response = await Client.PostAsJsonAsync(ExpensesRoute, new ExpenseCreateDTO
        {
            Date = invoiceDate,
            Description = "Charge",
            Value = value,
            CategoryId = Guid.Parse(MercadoCategoryId),
            CreditCardId = cardId,
            InvoiceDate = invoiceDate
        });
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    [Trait("AC", "P45-F02-credit-card-due-date-event-sync-01")]
    public async Task SavingAnActiveCardWithADueDate_CreatesExactlyOneEvent_ShowingNameDueDateAndBalance()
    {
        Connect();
        var cardId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-500000000004"); // BaAmex
        await AddChargeAsync(cardId, new DateOnly(2026, 9, 10), 55.5m);

        await SetDueDateAsync(cardId, "BaAmex", isActive: true, new DateOnly(2026, 9, 10));
        var status = await WaitForResolvedSyncStatusAsync(cardId);

        status!.State.Should().Be("Synced");
        _provider.CreatedEvents.Should().ContainSingle();
        _provider.CreatedEvents[0].Title.Should().Be("BaAmex — Due 55.50");
        _provider.CreatedEvents[0].Description.Should().Contain("10 September 2026");
        _provider.CreatedEvents[0].Date.Should().Be(new DateOnly(2026, 9, 10));
    }

    [Fact]
    [Trait("AC", "P45-F02-credit-card-due-date-event-sync-02")]
    public async Task ChangingTheDueDate_UpdatesTheSameEvent_InsteadOfCreatingASecondOne()
    {
        Connect();
        var cardId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-500000000004"); // BaAmex
        await SetDueDateAsync(cardId, "BaAmex", isActive: true, new DateOnly(2026, 9, 10));
        await WaitForResolvedSyncStatusAsync(cardId);

        await SetDueDateAsync(cardId, "BaAmex", isActive: true, new DateOnly(2026, 9, 20));
        await WaitForResolvedSyncStatusAsync(cardId);

        _provider.CreatedEvents.Should().ContainSingle();
        _provider.UpdatedEvents.Should().ContainSingle(e => e.EventId == _provider.CreatedEventId && e.Date == new DateOnly(2026, 9, 20));
    }

    [Fact]
    [Trait("AC", "P45-F02-credit-card-due-date-event-sync-03")]
    public async Task ClearingDueDateDeactivatingOrDeletingACard_RemovesItsEvent()
    {
        Connect();
        var clearedCardId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-500000000004"); // BaAmex
        var deactivatedCardId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-500000000003"); // ChaseMaster4023
        var deletedCardId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-500000000005"); // PaypalCredit

        foreach (var (id, name) in new[] { (clearedCardId, "BaAmex"), (deactivatedCardId, "ChaseMaster4023"), (deletedCardId, "PaypalCredit") })
        {
            await SetDueDateAsync(id, name, isActive: true, new DateOnly(2026, 9, 10));
            await WaitForResolvedSyncStatusAsync(id);
        }

        _provider.CreatedEvents.Should().HaveCount(3);

        await SetDueDateAsync(clearedCardId, "BaAmex", isActive: true, dueDate: null);
        await WaitForResolvedSyncStatusAsync(clearedCardId);

        await SetDueDateAsync(deactivatedCardId, "ChaseMaster4023", isActive: false, new DateOnly(2026, 9, 10));
        await WaitForResolvedSyncStatusAsync(deactivatedCardId);

        (await Client.DeleteAsync($"{CreditCardsRoute}/{deletedCardId}")).EnsureSuccessStatusCode();
        await WaitForResolvedSyncStatusAsync(deletedCardId);

        _provider.DeletedEvents.Should().HaveCount(3);
    }

    [Fact]
    [Trait("AC", "P45-F02-credit-card-due-date-event-sync-05")]
    public async Task ACardWithNoPostedChargesForThePeriod_ShowsZeroBalanceWithTheNote()
    {
        Connect();
        var cardId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-500000000002"); // BarclaysPlatinumVisa6007, no charges added

        await SetDueDateAsync(cardId, "BarclaysPlatinumVisa6007", isActive: true, new DateOnly(2026, 9, 10));
        await WaitForResolvedSyncStatusAsync(cardId);

        _provider.CreatedEvents.Should().ContainSingle();
        _provider.CreatedEvents[0].Title.Should().Contain("0.00");
        _provider.CreatedEvents[0].Description.Should().Contain("(no charges posted to this invoice yet)");
    }

    [Fact]
    [Trait("AC", "P45-F02-credit-card-due-date-event-sync-06")]
    public async Task ASimulatedCalendarApiFailure_LeavesTheSaveUnaffected_AndMarksErrorWithRetryAvailable()
    {
        Connect();
        var cardId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-500000000004"); // BaAmex
        _provider.CreateEventThrows = true;

        var response = await Client.PutAsJsonAsync($"{CreditCardsRoute}/{cardId}",
            new CreditCardUpdateDTO { Name = "BaAmex", IsActive = true, NextInvoiceDueDate = new DateOnly(2026, 9, 10) });
        response.EnsureSuccessStatusCode();
        var status = await WaitForResolvedSyncStatusAsync(cardId);

        status!.State.Should().Be("Error");
        status.LastError.Should().NotBeNullOrEmpty();

        _provider.CreateEventThrows = false;
        var retryResponse = await Client.PostAsync($"{CalendarRoute}/credit-cards/{cardId}/resync", content: null);
        var retryResult = await retryResponse.Content.ReadFromJsonAsync<CreditCardCalendarSyncStatusDTO>();

        retryResult!.State.Should().Be("Synced");
    }

    [Fact]
    [Trait("AC", "P45-F02-credit-card-due-date-event-sync-07")]
    public async Task DeletingTheDedicatedCalendarExternally_RecreatesItAndResyncsEveryActiveCard()
    {
        Connect("old-cal");
        var card1 = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-500000000004"); // BaAmex
        var card2 = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-500000000001"); // BarclaysPlatinumVisa8003
        await SetDueDateAsync(card1, "BaAmex", isActive: true, new DateOnly(2026, 9, 10));
        await WaitForResolvedSyncStatusAsync(card1);
        await SetDueDateAsync(card2, "BarclaysPlatinumVisa8003", isActive: true, new DateOnly(2026, 9, 12));
        await WaitForResolvedSyncStatusAsync(card2);

        _provider.NotFoundCalendarId = "old-cal";
        _provider.CreatedCalendarId = "new-cal";

        var response = await Client.PostAsync($"{CalendarRoute}/resync-all", content: null);
        response.EnsureSuccessStatusCode();

        var connection = Services.GetRequiredService<ICalendarConnectionStore>().Load();
        connection!.CalendarId.Should().Be("new-cal");
        connection.CardEventIds.Should().ContainKeys(card1, card2);
        _provider.CreatedEvents.Should().Contain(e => e.CalendarId == "new-cal");
    }
}
