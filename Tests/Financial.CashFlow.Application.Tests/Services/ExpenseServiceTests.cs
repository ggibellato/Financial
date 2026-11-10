using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

[Trait("Category", "Unit")]
public class ExpenseServiceTests
{
    private static readonly Account ChaseFixture = TestAccounts.Bank("Chase", roundUpEnabled: true);
    private static readonly Account BarclaysPlatinumVisa8003Fixture = TestAccounts.CreditCard("BarclaysPlatinumVisa8003");
    private static readonly Account BaAmexFixture = TestAccounts.CreditCard("BaAmex");
    private static readonly Category MercadoFixture = Category.Create("Mercado");
    private static readonly Microsoft.Extensions.Logging.ILogger<ExpenseService> Logger = NullLogger<ExpenseService>.Instance;

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly TimeProvider _clock = TestClock.At(new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero));
    private readonly FakeCreditCardCalendarSyncService _calendarSyncTrigger = new();
    private readonly ExpenseService _sut;

    public ExpenseServiceTests()
    {
        _repository = CreateRepository();
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    private static StubCashFlowRepository CreateRepository(
        bool seedDefaultCreditCards = true, bool seedDefaultCategories = true) =>
        new(seedDefaultBanks: true, seedDefaultCreditCards: seedDefaultCreditCards, seedDefaultCategories: seedDefaultCategories);

    private ExpenseService CreateService(
        StubCashFlowRepository? repository = null,
        Microsoft.Extensions.Logging.ILogger<ExpenseService>? logger = null) =>
        new(repository ?? _repository, _clock, _tracer, logger ?? Logger, _calendarSyncTrigger);

    [Fact]
    public async Task AddExpenseAsync_NegativeCardChargeDatedAfterToday_ThrowsWithoutSaving()
    {
        var act = () => _sut.AddExpenseAsync(ToCreateDto(_repository, CardExpenseRequest(new DateOnly(2026, 11, 1)) with { Date = new DateOnly(2026, 11, 6), Value = -40m }));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Date must not be in the future.");
        using (new AssertionScope())
        {
            _repository.Expenses.Should().BeEmpty();
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ExpenseService.AddExpense");
        }
    }

    [Fact]
    public async Task AddExpenseAsync_NegativeCardChargeDatedToday_IsSaved()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, CardExpenseRequest(new DateOnly(2026, 11, 1)) with { Date = new DateOnly(2026, 11, 5), Value = -40m }));

        added.Value.Should().Be(-40m);
    }

    [Fact]
    public async Task AddExpenseAsync_PositiveCardChargeDatedAfterToday_IsSaved()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, CardExpenseRequest(new DateOnly(2026, 11, 1)) with { Date = new DateOnly(2026, 11, 6), Value = 40m }));

        added.Value.Should().Be(40m);
    }

    [Fact]
    public async Task UpdateExpenseAsync_NegativeCardChargeMovedAfterToday_ThrowsAndKeepsItsDate()
    {
        var request = CardExpenseRequest(new DateOnly(2026, 11, 1)) with { Date = new DateOnly(2026, 11, 3), Value = -40m };
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, request));

        var act = () => _sut.UpdateExpenseAsync(added.Id, ToUpdateDto(_repository, request with { Date = new DateOnly(2026, 11, 6) }));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Date must not be in the future.");
        using (new AssertionScope())
        {
            _repository.Expenses.Single(e => e.Id == added.Id).Date.Should().Be(new DateOnly(2026, 11, 3));
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ExpenseService.UpdateExpense");
        }
    }

    [Fact]
    public async Task UpdateExpenseAsync_FutureCardChargeTurnedNegative_Throws()
    {
        var request = CardExpenseRequest(new DateOnly(2026, 11, 1)) with { Date = new DateOnly(2026, 11, 6), Value = 40m };
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, request));

        var act = () => _sut.UpdateExpenseAsync(added.Id, ToUpdateDto(_repository, request with { Value = -40m }));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Date must not be in the future.");
        using (new AssertionScope())
        {
            _repository.Expenses.Single(e => e.Id == added.Id).Value.Should().Be(40m);
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ExpenseService.UpdateExpense");
        }
    }

    [Fact]
    public async Task AddExpenseAsync_NegativeBankExpenseDatedAfterToday_IsNotRefusedAsARefund()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest() with { Date = new DateOnly(2026, 11, 6), Value = -40m }));

        added.Value.Should().Be(-40m);
    }

    [Fact]
    public async Task AddExpenseAsync_CardExpense_ChecksItsInvoiceForCalendarSync()
    {
        var card = _repository.CardAccounts.Single(c => c.Name == "BaAmex");

        await _sut.AddExpenseAsync(ToCreateDto(_repository, CardExpenseRequest(new DateOnly(2026, 9, 1))));

        _calendarSyncTrigger.InvoiceChecks.Should().Equal((card.Id, new DateOnly(2026, 9, 1)));
    }

    [Fact]
    public async Task AddExpenseAsync_BankExpense_DoesNotCheckCalendarSync()
    {
        await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest()));

        _calendarSyncTrigger.InvoiceChecks.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateExpenseAsync_SameInvoice_ChecksCalendarSyncOnce()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, CardExpenseRequest(new DateOnly(2026, 9, 1))));
        _calendarSyncTrigger.InvoiceChecks.Clear();

        await _sut.UpdateExpenseAsync(added.Id, ToUpdateDto(_repository, CardExpenseRequest(new DateOnly(2026, 9, 1)) with { Value = added.Value + 10m }));

        _calendarSyncTrigger.InvoiceChecks.Should().ContainSingle();
    }

    [Fact]
    public async Task UpdateExpenseAsync_InvoiceMoved_ChecksBothOldAndNewInvoice()
    {
        var card = _repository.CardAccounts.Single(c => c.Name == "BaAmex");
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, CardExpenseRequest(new DateOnly(2026, 9, 1))));
        _calendarSyncTrigger.InvoiceChecks.Clear();

        await _sut.UpdateExpenseAsync(added.Id, ToUpdateDto(_repository, CardExpenseRequest(new DateOnly(2026, 10, 1))));

        _calendarSyncTrigger.InvoiceChecks.Should().Equal((card.Id, new DateOnly(2026, 9, 1)), (card.Id, new DateOnly(2026, 10, 1)));
    }

    [Fact]
    public async Task DeleteExpenseAsync_CardExpense_ChecksItsInvoiceForCalendarSync()
    {
        var card = _repository.CardAccounts.Single(c => c.Name == "BaAmex");
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, CardExpenseRequest(new DateOnly(2026, 9, 1))));
        _calendarSyncTrigger.InvoiceChecks.Clear();

        await _sut.DeleteExpenseAsync(added.Id);

        _calendarSyncTrigger.InvoiceChecks.Should().Equal((card.Id, new DateOnly(2026, 9, 1)));
    }

    [Fact]
    public async Task UpdateExpenseAsync_ExpenseOfAPotOperation_ThrowsLinkedOperationConflictAndKeepsIt()
    {
        var linked = AddLinkedExpense();

        var act = async () => await _sut.UpdateExpenseAsync(linked.Id, ToUpdateDto(_repository, ValidCreateRequest()));

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<LinkedOperationConflictException>()
                .WithMessage("This expense belongs to a pot deposit or withdrawal; edit it from the Pots account.");
            linked.Value.Should().Be(38.25m);
            linked.Description.Should().Be("Boots");
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<LinkedOperationConflictException>("CashFlow.ExpenseService.UpdateExpense");
        }
    }

    [Fact]
    public async Task DeleteExpenseAsync_ExpenseOfAPotOperation_ThrowsLinkedOperationConflictAndKeepsIt()
    {
        var linked = AddLinkedExpense();

        var act = async () => await _sut.DeleteExpenseAsync(linked.Id);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<LinkedOperationConflictException>()
                .WithMessage("This expense belongs to a pot deposit or withdrawal; edit it from the Pots account.");
            _repository.Expenses.Should().ContainSingle().Which.Should().BeSameAs(linked);
            _tracer.ShouldHaveFailedSpan<LinkedOperationConflictException>("CashFlow.ExpenseService.DeleteExpense");
        }
    }

    private Expense AddLinkedExpense()
    {
        var linked = Expense.Create(
            new DateOnly(2026, 10, 12), "Boots", 38.25m, _repository.Categories.First(), _repository.Accounts.First(a => a.Name == "Chase"),
            creditCard: null, operationId: Guid.NewGuid());
        _repository.Expenses.Add(linked);
        return linked;
    }

    private static ExpenseCreateRequest CardExpenseRequest(DateOnly invoiceDate) =>
        ValidCreateRequest() with { PaymentSource = null, CardTag = "BaAmex", InvoiceDate = invoiceDate };

    [Fact]
    public async Task AddExpenseAsync_WithValidRequest_RecordsSuccessfulSpan()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest());

        var result = await _sut.AddExpenseAsync(request);

        var span = _tracer.Spans.Should().ContainSingle().Which;
        span.Name.Should().Be("CashFlow.ExpenseService.AddExpense");
        span.Attributes[TelemetryAttributeKeys.BoundedContext].Should().Be("CashFlow");
        span.Attributes[TelemetryAttributeKeys.EntityType].Should().Be("Expense");
        span.Attributes[TelemetryAttributeKeys.EntityId].Should().Be(result.Id.ToString());
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
        span.RecordedException.Should().BeNull();
    }

    [Fact]
    public async Task AddExpenseAsync_WithZeroValue_RecordsFailedSpanWithException()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { Value = 0m });

        var act = async () => await _sut.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
        _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ExpenseService.AddExpense");
    }

    [Fact]
    public async Task AddExpenseAsync_WithValidRequest_SavesAndReturnsExpense()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest());

        var result = await _sut.AddExpenseAsync(request);

        using (new AssertionScope())
        {
            result.Description.Should().Be("Weekly groceries");
            result.Value.Should().Be(54.32m);
            result.CategoryName.Should().Be("Mercado");
            result.PaymentSourceBankName.Should().Be("Barclays");
            result.CreditCardName.Should().BeNull();
            result.PaymentStatus.Should().Be("ImmediatePayment");
            _repository.Expenses.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task AddExpenseAsync_WithoutCountsAsTithe_DefaultsToTrue()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { Category = "Dizimo" });

        var result = await _sut.AddExpenseAsync(request);

        result.CountsAsTithe.Should().BeTrue();
    }

    [Fact]
    public async Task AddExpenseAsync_WithCountsAsTitheFalse_SavesFalse()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { Category = "Dizimo", CountsAsTithe = false });

        var result = await _sut.AddExpenseAsync(request);

        result.CountsAsTithe.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateExpenseAsync_TogglingCountsAsTithe_UpdatesValue()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest() with { Category = "Dizimo" }));

        var updateRequest = ToUpdateDto(_repository, ValidCreateRequest() with { Category = "Dizimo", CountsAsTithe = false });
        var result = await _sut.UpdateExpenseAsync(added.Id, updateRequest);

        result.CountsAsTithe.Should().BeFalse();
    }

    [Fact]
    public async Task AddExpenseAsync_WithCardTagAndNoPaymentSource_SavesAsCreditCardCharge()
    {
        var request = ValidCreateRequest() with { PaymentSource = null, CardTag = "BarclaysPlatinumVisa8003" };

        var result = await _sut.AddExpenseAsync(ToCreateDto(_repository, request));

        using (new AssertionScope())
        {
            result.CreditCardName.Should().Be("BarclaysPlatinumVisa8003");
            result.PaymentSourceBankName.Should().BeNull();
            result.PaymentStatus.Should().Be("CreditCardCharge");
        }
    }

    [Fact]
    public async Task AddExpenseAsync_WithNeitherPaymentSourceNorCardTag_ThrowsArgumentException()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { PaymentSource = null, CardTag = null });

        var act = async () => await _sut.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*payment source or a card tag*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithBothPaymentSourceAndCardTag_ThrowsArgumentException()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { CardTag = "BarclaysPlatinumVisa8003" });

        var act = async () => await _sut.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*marking its card statement paid*");
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithBothPaymentSourceAndCardTag_ThrowsArgumentException()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest()));
        var updateRequest = ToUpdateDto(_repository, ValidCreateRequest() with { CardTag = "BaAmex" });

        var act = async () => await _sut.UpdateExpenseAsync(added.Id, updateRequest);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*marking its card statement paid*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithZeroValue_ThrowsArgumentException()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { Value = 0m });

        var act = async () => await _sut.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*zero*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithMissingCategory_ThrowsArgumentException()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { Category = "NotACategory" });

        var act = async () => await _sut.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Category*not recognized*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithInvalidPaymentSource_ThrowsArgumentException()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { PaymentSource = "NotASource" });

        var act = async () => await _sut.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Payment source*not recognized*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithInvalidCardTag_ThrowsArgumentException()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { CardTag = "NotACard" });

        var act = async () => await _sut.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Credit card*not recognized*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithInactiveCard_ThrowsArgumentException()
    {
        var repository = CreateRepository(seedDefaultCreditCards: false);
        var inactiveCard = TestAccounts.CreditCard("RetiredCard", isActive: false);
        repository.Accounts.Add(inactiveCard);
        var service = CreateService(repository);
        var request = new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 15),
            Description = "Weekly groceries",
            Value = 54.32m,
            CategoryId = ResolveCategoryId(repository, "Mercado"),
            CreditCardId = inactiveCard.Id
        };

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*RetiredCard*inactive*cannot be used for new entries*");
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithInactiveCard_ThrowsArgumentException()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest()));
        var inactiveCard = TestAccounts.CreditCard("RetiredCard", isActive: false);
        _repository.Accounts.Add(inactiveCard);
        var updateRequest = new ExpenseUpdateDTO
        {
            Date = new DateOnly(2026, 7, 15),
            Description = "Weekly groceries",
            Value = 54.32m,
            CategoryId = ResolveCategoryId(_repository, "Mercado"),
            CreditCardId = inactiveCard.Id
        };

        var act = async () => await _sut.UpdateExpenseAsync(added.Id, updateRequest);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*RetiredCard*inactive*cannot be used for new entries*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithInactiveCategory_ThrowsArgumentException()
    {
        var repository = CreateRepository(seedDefaultCreditCards: false, seedDefaultCategories: false);
        var inactiveCategory = Category.Create("RetiredCategory", isActive: false);
        repository.Categories.Add(inactiveCategory);
        var service = CreateService(repository);
        var request = new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 15),
            Description = "Weekly groceries",
            Value = 54.32m,
            CategoryId = inactiveCategory.Id,
            PaymentSourceBankId = repository.Accounts.First(b => b.Name == "Barclays").Id
        };

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*RetiredCategory*inactive*cannot be used for new entries*");
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithInactiveCategory_ThrowsArgumentException()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest()));
        var inactiveCategory = Category.Create("RetiredCategory", isActive: false);
        _repository.Categories.Add(inactiveCategory);
        var updateRequest = new ExpenseUpdateDTO
        {
            Date = new DateOnly(2026, 7, 15),
            Description = "Weekly groceries",
            Value = 54.32m,
            CategoryId = inactiveCategory.Id,
            PaymentSourceBankId = _repository.Accounts.First(b => b.Name == "Barclays").Id
        };

        var act = async () => await _sut.UpdateExpenseAsync(added.Id, updateRequest);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*RetiredCategory*inactive*cannot be used for new entries*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithBlankDescription_ThrowsArgumentException()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { Description = "  " });

        var act = async () => await _sut.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Description is required*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithDescriptionOver200Characters_ThrowsArgumentException()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { Description = new string('a', 201) });

        var act = async () => await _sut.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*200 characters*");
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithInvalidPaymentSource_ThrowsArgumentException()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest()));
        var updateRequest = ToUpdateDto(_repository, ValidCreateRequest() with { PaymentSource = "NotASource" });

        var act = async () => await _sut.UpdateExpenseAsync(added.Id, updateRequest);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Payment source*not recognized*");
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithExistingId_UpdatesInPlace()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest()));

        var updateRequest = new ExpenseUpdateDTO
        {
            Date = new DateOnly(2026, 8, 1),
            Description = "Updated",
            Value = 10m,
            CategoryId = ResolveCategoryId(_repository, "Casa"),
            PaymentSourceBankId = _repository.Accounts.First(b => b.Name == "Chase").Id,
            CreditCardId = null
        };
        var result = await _sut.UpdateExpenseAsync(added.Id, updateRequest);

        using (new AssertionScope())
        {
            result.Id.Should().Be(added.Id);
            result.Description.Should().Be("Updated");
            result.CategoryName.Should().Be("Casa");
            _repository.Expenses.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(2);
        }
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var updateRequest = ToUpdateDto(_repository, ValidCreateRequest());

        var act = async () => await _sut.UpdateExpenseAsync(Guid.NewGuid(), updateRequest);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteExpenseAsync_WithExistingId_RemovesAndSaves()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest()));

        await _sut.DeleteExpenseAsync(added.Id);

        _repository.Expenses.Should().BeEmpty();
        _repository.SaveChangesCallCount.Should().Be(2);
    }

    [Fact]
    public async Task DeleteExpenseAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.DeleteExpenseAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetCategoryTotalsByMonth_SumsValuesPerCategoryForThatMonth()
    {
        await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest() with { Category = "Mercado", Value = 10m }));
        await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest() with { Category = "Mercado", Value = 5m }));
        await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest() with { Category = "Casa", Value = 20m }));

        var result = _sut.GetCategoryTotalsByMonth(2026, 7);

        using (new AssertionScope())
        {
            result.Should().HaveCount(2);
            result.Should().ContainSingle(t => t.Category == "Mercado" && t.TotalValue == 15m);
            result.Should().ContainSingle(t => t.Category == "Casa" && t.TotalValue == 20m);
        }
    }

    [Fact]
    public async Task GetCategoryTotalsByMonth_NegativeValue_CountsTowardTotal()
    {
        await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest() with { Category = "Reserva", Value = 100m }));
        await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest() with { Category = "Reserva", Value = -30m }));

        var result = _sut.GetCategoryTotalsByMonth(2026, 7);

        result.Should().ContainSingle(t => t.Category == "Reserva" && t.TotalValue == 70m);
    }

    [Fact]
    public void GetCategoryTotalsByMonth_UnpaidCardCharge_CountsTowardInvoiceMonthNotChargeMonth()
    {
        var charge = Expense.Create(
            new DateOnly(2026, 7, 29), "Cutoff charge", 40m, MercadoFixture, null,
           BarclaysPlatinumVisa8003Fixture, new DateOnly(2026, 8, 1));
        _repository.Expenses.Add(charge);

        var julyResult = _sut.GetCategoryTotalsByMonth(2026, 7);
        var augustResult = _sut.GetCategoryTotalsByMonth(2026, 8);

        using (new AssertionScope())
        {
            julyResult.Should().BeEmpty();
            augustResult.Should().ContainSingle(t => t.Category == "Mercado" && t.TotalValue == 40m);
        }
    }

    [Fact]
    public void GetCategoryTotalsByMonth_SettledCardCharge_CountsTowardStatementMonth()
    {
        var charge = Expense.Create(new DateOnly(2026, 7, 10), "Settled charge", 40m, MercadoFixture, null, BarclaysPlatinumVisa8003Fixture);
        charge.Settle(ChaseFixture, new DateOnly(2026, 8, 3));
        _repository.Expenses.Add(charge);

        var julyResult = _sut.GetCategoryTotalsByMonth(2026, 7);
        var augustResult = _sut.GetCategoryTotalsByMonth(2026, 8);

        using (new AssertionScope())
        {
            julyResult.Should().ContainSingle(t => t.Category == "Mercado" && t.TotalValue == 40m);
            augustResult.Should().BeEmpty();
        }
    }

    [Fact]
    public void GetCategoryTotalsByMonth_MixOfUnpaidSettledAndBank_NoExpenseCountedInMoreThanOneMonth()
    {
        var unpaidCutoff = Expense.Create(
            new DateOnly(2026, 7, 29), "Unpaid cutoff", 10m, MercadoFixture, null,
           BarclaysPlatinumVisa8003Fixture, new DateOnly(2026, 8, 1));
        var settled = Expense.Create(new DateOnly(2026, 7, 12), "Settled", 20m, MercadoFixture, null, BaAmexFixture);
        settled.Settle(ChaseFixture, new DateOnly(2026, 7, 20));
        var bank = Expense.Create(new DateOnly(2026, 7, 15), "Bank", 30m, MercadoFixture, ChaseFixture, null);
        _repository.Expenses.Add(unpaidCutoff);
        _repository.Expenses.Add(settled);
        _repository.Expenses.Add(bank);

        var julyTotal = _sut.GetCategoryTotalsByMonth(2026, 7).Sum(t => t.TotalValue);
        var augustTotal = _sut.GetCategoryTotalsByMonth(2026, 8).Sum(t => t.TotalValue);

        using (new AssertionScope())
        {
            julyTotal.Should().Be(50m);
            augustTotal.Should().Be(10m);
            (julyTotal + augustTotal).Should().Be(60m);
        }
    }

    [Fact]
    public async Task AddExpenseAsync_WithRoundUpAmountOnRoundUpEnabledBank_SavesAmount()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { PaymentSource = "Trading212", Value = 9.40m, RoundUpAmount = 0.60m });

        var result = await _sut.AddExpenseAsync(request);

        result.RoundUpAmount.Should().Be(0.60m);
    }

    [Fact]
    public async Task AddExpenseAsync_WithRoundUpAmountOfZero_SavesExplicitZero()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { PaymentSource = "Trading212", Value = 10.00m, RoundUpAmount = 0.00m });

        var result = await _sut.AddExpenseAsync(request);

        result.RoundUpAmount.Should().Be(0.00m);
    }

    [Fact]
    public async Task AddExpenseAsync_WithRoundUpAmountOnNonRoundUpBank_ThrowsNamingTheBank()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { PaymentSource = "Barclays", RoundUpAmount = 0.50m });

        var act = async () => await _sut.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Barclays*does not support round-up*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithRoundUpAmountOnCreditCardTaggedExpense_Throws()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { PaymentSource = null, CardTag = "ChaseMaster4023", RoundUpAmount = 0.50m });

        var act = async () => await _sut.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*not a credit-card charge*");
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.00)]
    public async Task AddExpenseAsync_WithRoundUpAmountOutsideRange_Throws(decimal roundUpAmount)
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { PaymentSource = "Chase", RoundUpAmount = roundUpAmount });

        var act = async () => await _sut.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*between £0.00 and £0.99*");
    }

    [Fact]
    public async Task AddExpenseAsync_EligibleWithNoRoundUpAmount_ReturnsSuggestedAmount()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { PaymentSource = "Trading212", Value = 9.40m });

        var result = await _sut.AddExpenseAsync(request);

        result.RoundUpAmount.Should().BeNull();
        result.SuggestedRoundUpAmount.Should().Be(0.60m);
    }

    [Fact]
    public async Task AddExpenseAsync_EligibleWithRoundUpAmountAlreadySaved_ReturnsNoSuggestion()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { PaymentSource = "Trading212", Value = 9.40m, RoundUpAmount = 0.60m });

        var result = await _sut.AddExpenseAsync(request);

        result.SuggestedRoundUpAmount.Should().BeNull();
    }

    [Fact]
    public async Task AddExpenseAsync_OnNonRoundUpBank_ReturnsNoSuggestion()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { PaymentSource = "Barclays", Value = 9.40m });

        var result = await _sut.AddExpenseAsync(request);

        result.SuggestedRoundUpAmount.Should().BeNull();
    }

    [Fact]
    public async Task AddExpenseAsync_CreditCardCharge_ReturnsNoSuggestion()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { PaymentSource = null, CardTag = "ChaseMaster4023", Value = 9.40m });

        var result = await _sut.AddExpenseAsync(request);

        result.SuggestedRoundUpAmount.Should().BeNull();
    }

    [Fact]
    public async Task AddExpenseAsync_NegativeValueOnRoundUpBank_ReturnsNoSuggestion()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { PaymentSource = "Trading212", Value = -9.40m });

        var result = await _sut.AddExpenseAsync(request);

        result.SuggestedRoundUpAmount.Should().BeNull();
    }

    [Fact]
    public async Task AddExpenseAsync_WithRoundUpAmountOnNegativeValue_Throws()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { PaymentSource = "Trading212", Value = -9.40m, RoundUpAmount = 0.60m });

        var act = async () => await _sut.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*negative (reimbursement) expense*");
    }

    [Fact]
    public async Task UpdateExpenseAsync_ChangingValueOnly_LeavesRoundUpAmountUnchanged()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, 
            ValidCreateRequest() with { PaymentSource = "Trading212", Value = 9.40m, RoundUpAmount = 0.60m }));

        var updateRequest = ToUpdateDto(_repository, 
            ValidCreateRequest() with { PaymentSource = "Trading212", Value = 20m, RoundUpAmount = 0.60m });
        var result = await _sut.UpdateExpenseAsync(added.Id, updateRequest);

        result.Value.Should().Be(20m);
        result.RoundUpAmount.Should().Be(0.60m);
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithNewRoundUpAmount_ChangesIt()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, 
            ValidCreateRequest() with { PaymentSource = "Trading212", RoundUpAmount = 0.60m }));

        var updateRequest = ToUpdateDto(_repository, ValidCreateRequest() with { PaymentSource = "Trading212", RoundUpAmount = 0.10m });
        var result = await _sut.UpdateExpenseAsync(added.Id, updateRequest);

        result.RoundUpAmount.Should().Be(0.10m);
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithNullRoundUpAmount_ClearsAPreviouslySavedAmount()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, 
            ValidCreateRequest() with { PaymentSource = "Trading212", RoundUpAmount = 0.60m }));

        var updateRequest = ToUpdateDto(_repository, ValidCreateRequest() with { PaymentSource = "Trading212", RoundUpAmount = null });
        var result = await _sut.UpdateExpenseAsync(added.Id, updateRequest);

        result.RoundUpAmount.Should().BeNull();
    }

    private static ExpenseCreateRequest ValidCreateRequest() => new(
        new DateOnly(2026, 7, 15),
        "Weekly groceries",
        54.32m,
        "Mercado",
        "Barclays",
        null);

    private static ExpenseCreateDTO ToCreateDto(StubCashFlowRepository _repository, ExpenseCreateRequest r) => new()
    {
        Date = r.Date,
        Description = r.Description,
        Value = r.Value,
        CategoryId = ResolveCategoryId(_repository, r.Category),
        PaymentSourceBankId = ResolveBankId(_repository, r.PaymentSource),
        CreditCardId = ResolveCreditCardId(_repository, r.CardTag),
        InvoiceDate = r.InvoiceDate,
        RoundUpAmount = r.RoundUpAmount,
        CountsAsTithe = r.CountsAsTithe
    };

    private static ExpenseUpdateDTO ToUpdateDto(StubCashFlowRepository _repository, ExpenseCreateRequest r) => new()
    {
        Date = r.Date,
        Description = r.Description,
        Value = r.Value,
        CategoryId = ResolveCategoryId(_repository, r.Category),
        PaymentSourceBankId = ResolveBankId(_repository, r.PaymentSource),
        CreditCardId = ResolveCreditCardId(_repository, r.CardTag),
        InvoiceDate = r.InvoiceDate,
        RoundUpAmount = r.RoundUpAmount,
        CountsAsTithe = r.CountsAsTithe
    };

    /// <summary>An unresolvable name maps to a random, never-seeded Guid so tests exercising an unrecognized reference still hit the "not found" path rather than the "omitted" path.</summary>
    private static Guid? ResolveBankId(StubCashFlowRepository _repository, string? bankName) =>
        bankName is null ? null : _repository.Accounts.FirstOrDefault(b => b.Name == bankName)?.Id ?? Guid.NewGuid();

    /// <summary>An unresolvable name maps to a random, never-seeded Guid so tests exercising an unrecognized reference still hit the "not found" path rather than the "omitted" path.</summary>
    private static Guid? ResolveCreditCardId(StubCashFlowRepository _repository, string? cardName) =>
        cardName is null ? null : _repository.CardAccounts.FirstOrDefault(c => c.Name == cardName)?.Id ?? Guid.NewGuid();

    /// <summary>An unresolvable name maps to a random, never-seeded Guid so tests exercising an unrecognized reference still hit the "not found" path rather than the "omitted" path.</summary>
    private static Guid ResolveCategoryId(StubCashFlowRepository _repository, string categoryName) =>
        _repository.Categories.FirstOrDefault(c => c.Name == categoryName)?.Id ?? Guid.NewGuid();

    private sealed record ExpenseCreateRequest(
        DateOnly Date, string Description, decimal Value, string Category, string? PaymentSource, string? CardTag,
        decimal? RoundUpAmount = null, DateOnly? InvoiceDate = null, bool CountsAsTithe = true);

    [Fact]
    public async Task AddExpenseAsync_CreditCardExpense_ReturnsNonNullChargeDateAndInvoiceDate()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { PaymentSource = null, CardTag = "ChaseMaster4023" });

        var result = await _sut.AddExpenseAsync(request);

        using (new AssertionScope())
        {
            result.ChargeDate.Should().NotBeNull();
            result.InvoiceDate.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task AddExpenseAsync_WithInvoiceDateOverride_UsesProvidedMonth()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with
        {
            Date = new DateOnly(2026, 7, 29),
            PaymentSource = null,
            CardTag = "ChaseMaster4023",
            InvoiceDate = new DateOnly(2026, 8, 17)
        });

        var result = await _sut.AddExpenseAsync(request);

        result.InvoiceDate.Should().Be(new DateOnly(2026, 8, 1));
    }

    [Fact]
    public async Task AddExpenseAsync_WithoutInvoiceDateOverride_DefaultsToChargeMonth()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with
        {
            Date = new DateOnly(2026, 7, 15), PaymentSource = null, CardTag = "ChaseMaster4023"
        });

        var result = await _sut.AddExpenseAsync(request);

        result.InvoiceDate.Should().Be(new DateOnly(2026, 7, 1));
    }

    [Fact]
    public async Task AddExpenseAsync_BankExpense_ChargeDateAndInvoiceDateAreNull()
    {
        var result = await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest()));

        using (new AssertionScope())
        {
            result.ChargeDate.Should().BeNull();
            result.InvoiceDate.Should().BeNull();
        }
    }

    [Fact]
    public async Task UpdateExpenseAsync_ChangingInvoiceDateWhileUnpaid_PersistsOverride()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, 
            ValidCreateRequest() with { PaymentSource = null, CardTag = "ChaseMaster4023" }));
        var updateRequest = ToUpdateDto(_repository, ValidCreateRequest() with
        {
            PaymentSource = null, CardTag = "ChaseMaster4023", InvoiceDate = new DateOnly(2026, 8, 12)
        });

        var result = await _sut.UpdateExpenseAsync(added.Id, updateRequest);

        result.InvoiceDate.Should().Be(new DateOnly(2026, 8, 1));
    }

    [Fact]
    public async Task UpdateExpenseAsync_EchoingUnchangedInvoiceDateOnSettledExpense_Succeeds()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, 
            ValidCreateRequest() with { PaymentSource = null, CardTag = "ChaseMaster4023" }));
        _repository.Expenses.Single(e => e.Id == added.Id).Settle(_repository.Accounts.First(b => b.Name == "Barclays"), new DateOnly(2026, 7, 31));
        var updateRequest = ToUpdateDto(_repository, ValidCreateRequest() with
        {
            Description = "Renamed", PaymentSource = "Barclays", CardTag = "ChaseMaster4023", InvoiceDate = added.InvoiceDate
        });

        var result = await _sut.UpdateExpenseAsync(added.Id, updateRequest);

        result.Description.Should().Be("Renamed");
    }

    [Fact]
    public async Task UpdateExpenseAsync_ChangingInvoiceDateOnSettledExpense_Throws()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, 
            ValidCreateRequest() with { PaymentSource = null, CardTag = "ChaseMaster4023" }));
        _repository.Expenses.Single(e => e.Id == added.Id).Settle(_repository.Accounts.First(b => b.Name == "Barclays"), new DateOnly(2026, 7, 31));
        var updateRequest = ToUpdateDto(_repository, ValidCreateRequest() with
        {
            PaymentSource = "Barclays", CardTag = "ChaseMaster4023", InvoiceDate = new DateOnly(2026, 9, 1)
        });

        var act = async () => await _sut.UpdateExpenseAsync(added.Id, updateRequest);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*unsettled credit card charge*");
    }

    [Fact]
    public async Task AddExpenseAsync_LogsEntryAndSuccess_WithOperationNameOnly()
    {
        var logger = new RecordingLogger<ExpenseService>();
        var service = CreateService(logger: logger);
        var request = ToCreateDto(_repository, ValidCreateRequest());

        await service.AddExpenseAsync(request);

        logger.Entries.Should().Contain(e => e.Message == "AddExpense started");
        logger.Entries.Should().Contain(e => e.Message == "AddExpense completed");
        // Only the operation name is logged - never the expense's description or value (FR-014).
        logger.Entries.Should().OnlyContain(e => !e.Message.Contains(request.Description));
    }

    [Fact]
    public void GetCategoryTotalsByMonth_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnNextRead = new InvalidOperationException("simulated failure");

        Action act = () => _sut.GetCategoryTotalsByMonth(2026, 7);

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.ExpenseService.GetCategoryTotalsByMonth");
    }

    private IEnumerable<(int Year, int Month)> StatementMonthsOf(string cardName) =>
        _repository.CardStatements.Where(s => s.CreditCard.Name == cardName).Select(s => (s.Year, s.Month));

    [Fact]
    public async Task AddExpenseAsync_CardChargesInAMonthWithoutAStatement_CreateItOnce()
    {
        await _sut.AddExpenseAsync(ToCreateDto(_repository, CardExpenseRequest(new DateOnly(2026, 9, 1))));
        await _sut.AddExpenseAsync(ToCreateDto(_repository, CardExpenseRequest(new DateOnly(2026, 9, 1)) with { Value = 5m }));

        using (new AssertionScope())
        {
            StatementMonthsOf("BaAmex").Should().Equal((2026, 9));
            _repository.CardStatements.Single().IsPaid.Should().BeFalse();
            _repository.CardStatements.Single().DueDate.Should().BeNull();
        }
    }

    [Fact]
    public async Task AddExpenseAsync_CardChargeInAMonthWithAStatement_KeepsItsDueDate()
    {
        var card = _repository.CardAccounts.Single(c => c.Name == "BaAmex");
        var statement = CardStatement.Create(card, 2026, 9);
        statement.SetDueDate(new DateOnly(2026, 9, 18));
        _repository.CardStatements.Add(statement);

        await _sut.AddExpenseAsync(ToCreateDto(_repository, CardExpenseRequest(new DateOnly(2026, 9, 1))));

        using (new AssertionScope())
        {
            _repository.CardStatements.Should().Equal(statement);
            statement.IsPaid.Should().BeFalse();
            statement.DueDate.Should().Be(new DateOnly(2026, 9, 18));
        }
    }

    private const string PaidStatementMessage = "This charge's statement is paid; unmark it first.";

    private CardStatement MarkStatementPaid(string cardName, int year, int month)
    {
        var card = _repository.CardAccounts.Single(c => c.Name == cardName);
        var statement = _repository.CardStatements.SingleOrDefault(s => s.IsFor(card.Id, year, month));
        if (statement is null)
        {
            statement = CardStatement.Create(card, year, month);
            _repository.CardStatements.Add(statement);
        }

        statement.MarkLegacyPaid();
        return statement;
    }

    private async Task<ExpenseDTO> AddChargeOnPaidStatementAsync()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, CardExpenseRequest(new DateOnly(2026, 9, 1))));
        MarkStatementPaid("BaAmex", 2026, 9);
        return added;
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-09")]
    public async Task AddExpenseAsync_ChargeOnAPaidStatement_ThrowsAndSavesNothing()
    {
        MarkStatementPaid("BaAmex", 2026, 9);

        var act = async () => await _sut.AddExpenseAsync(ToCreateDto(_repository, CardExpenseRequest(new DateOnly(2026, 9, 1))));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage(PaidStatementMessage);
        using (new AssertionScope())
        {
            _repository.Expenses.Should().BeEmpty();
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ExpenseService.AddExpense");
        }
    }

    [Fact]
    public async Task AddExpenseAsync_ChargeOnAnotherCardsPaidStatement_IsAdded()
    {
        MarkStatementPaid("BarclaysPlatinumVisa8003", 2026, 9);

        await _sut.AddExpenseAsync(ToCreateDto(_repository, CardExpenseRequest(new DateOnly(2026, 9, 1))));

        _repository.Expenses.Should().ContainSingle();
    }

    public static TheoryData<string> LockedChargeChanges => new() { "date", "value", "invoice month", "card", "bank" };

    [Theory]
    [MemberData(nameof(LockedChargeChanges))]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-09")]
    public async Task UpdateExpenseAsync_ChargeOnAPaidStatement_LockedFieldChange_ThrowsAndChangesNothing(string change)
    {
        var added = await AddChargeOnPaidStatementAsync();
        var request = CardExpenseRequest(new DateOnly(2026, 9, 1));
        request = change switch
        {
            "date" => request with { Date = request.Date.AddDays(1) },
            "value" => request with { Value = request.Value + 1m },
            "invoice month" => request with { InvoiceDate = new DateOnly(2026, 10, 1) },
            "card" => request with { CardTag = "BarclaysPlatinumVisa8003" },
            _ => request with { CardTag = null, InvoiceDate = null, PaymentSource = "Barclays" }
        };
        var savesBefore = _repository.SaveChangesCallCount;

        var act = async () => await _sut.UpdateExpenseAsync(added.Id, ToUpdateDto(_repository, request));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage(PaidStatementMessage);
        var expense = _repository.Expenses.Single();
        using (new AssertionScope())
        {
            expense.Date.Should().Be(added.Date);
            expense.Value.Should().Be(added.Value);
            expense.CreditCard!.Name.Should().Be("BaAmex");
            expense.InvoiceDate.Should().Be(new DateOnly(2026, 9, 1));
            _repository.SaveChangesCallCount.Should().Be(savesBefore);
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ExpenseService.UpdateExpense");
        }
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-09")]
    public async Task UpdateExpenseAsync_ChargeOnAPaidStatement_DescriptionCategoryAndTitheChange_IsSaved()
    {
        var added = await AddChargeOnPaidStatementAsync();
        var request = CardExpenseRequest(new DateOnly(2026, 9, 1)) with { Description = "Renamed", Category = "Casa", CountsAsTithe = false };

        var result = await _sut.UpdateExpenseAsync(added.Id, ToUpdateDto(_repository, request));

        using (new AssertionScope())
        {
            result.Description.Should().Be("Renamed");
            result.CategoryName.Should().Be("Casa");
            result.CountsAsTithe.Should().BeFalse();
            result.InvoiceDate.Should().Be(new DateOnly(2026, 9, 1));
        }
    }

    [Fact]
    public async Task UpdateExpenseAsync_ChargeOnAPaidStatement_WithoutInvoiceMonth_KeepsItsStatementAndIsSaved()
    {
        var added = await AddChargeOnPaidStatementAsync();
        var request = CardExpenseRequest(new DateOnly(2026, 9, 1)) with { Description = "Renamed", InvoiceDate = null };

        var result = await _sut.UpdateExpenseAsync(added.Id, ToUpdateDto(_repository, request));

        result.Description.Should().Be("Renamed");
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-09")]
    public async Task UpdateExpenseAsync_MovingAnUnpaidChargeIntoAPaidStatement_Throws()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, CardExpenseRequest(new DateOnly(2026, 10, 1))));
        MarkStatementPaid("BaAmex", 2026, 9);

        var act = async () => await _sut.UpdateExpenseAsync(added.Id, ToUpdateDto(_repository, CardExpenseRequest(new DateOnly(2026, 9, 1))));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage(PaidStatementMessage);
        _repository.Expenses.Single().InvoiceDate.Should().Be(new DateOnly(2026, 10, 1));
        _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ExpenseService.UpdateExpense");
    }

    [Fact]
    public async Task UpdateExpenseAsync_BankExpenseBecomingAChargeOnAPaidStatement_Throws()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest()));
        MarkStatementPaid("BaAmex", 2026, 9);

        var act = async () => await _sut.UpdateExpenseAsync(added.Id, ToUpdateDto(_repository, CardExpenseRequest(new DateOnly(2026, 9, 1))));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage(PaidStatementMessage);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(11)]
    public async Task UpdateExpenseAsync_UnpaidCharge_MovesToAnEarlierOrLaterUnpaidStatement(int month)
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, CardExpenseRequest(new DateOnly(2026, 9, 1))));
        MarkStatementPaid("BaAmex", 2026, 10);

        var result = await _sut.UpdateExpenseAsync(added.Id, ToUpdateDto(_repository, CardExpenseRequest(new DateOnly(2026, month, 1))));

        result.InvoiceDate.Should().Be(new DateOnly(2026, month, 1));
    }

    [Fact]
    public async Task UpdateExpenseAsync_LegacySettledChargeOnAPaidStatement_DateChange_Throws()
    {
        var added = await AddChargeOnPaidStatementAsync();
        var expense = _repository.Expenses.Single();
        expense.Settle(_repository.Accounts.Single(a => a.Name == "Barclays"), new DateOnly(2026, 10, 3));
        var request = CardExpenseRequest(new DateOnly(2026, 9, 1)) with { Date = new DateOnly(2026, 10, 4), PaymentSource = "Barclays" };

        var act = async () => await _sut.UpdateExpenseAsync(added.Id, ToUpdateDto(_repository, request));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage(PaidStatementMessage);
        expense.Date.Should().Be(new DateOnly(2026, 10, 3));
    }

    [Fact]
    public async Task UpdateExpenseAsync_LegacySettledChargeOnAPaidStatement_DescriptionChange_IsSaved()
    {
        var added = await AddChargeOnPaidStatementAsync();
        var expense = _repository.Expenses.Single();
        expense.Settle(_repository.Accounts.Single(a => a.Name == "Barclays"), new DateOnly(2026, 10, 3));
        var request = CardExpenseRequest(new DateOnly(2026, 9, 1)) with { Date = new DateOnly(2026, 10, 3), PaymentSource = "Barclays", Description = "Renamed" };

        var result = await _sut.UpdateExpenseAsync(added.Id, ToUpdateDto(_repository, request));

        using (new AssertionScope())
        {
            result.Description.Should().Be("Renamed");
            result.PaymentStatus.Should().Be(nameof(ExpensePaymentStatus.CreditCardSettled));
        }
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-09")]
    public async Task DeleteExpenseAsync_ChargeOnAPaidStatement_ThrowsAndKeepsTheCharge()
    {
        var added = await AddChargeOnPaidStatementAsync();
        var savesBefore = _repository.SaveChangesCallCount;

        var act = async () => await _sut.DeleteExpenseAsync(added.Id);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage(PaidStatementMessage);
        using (new AssertionScope())
        {
            _repository.Expenses.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(savesBefore);
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ExpenseService.DeleteExpense");
        }
    }

    [Fact]
    public async Task DeleteExpenseAsync_ChargeOnAnUnpaidStatement_IsDeleted()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, CardExpenseRequest(new DateOnly(2026, 9, 1))));
        MarkStatementPaid("BaAmex", 2026, 10);

        await _sut.DeleteExpenseAsync(added.Id);

        _repository.Expenses.Should().BeEmpty();
    }

    [Fact]
    public async Task AddExpenseAsync_BankExpense_CreatesNoStatement()
    {
        await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest()));

        _repository.CardStatements.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateExpenseAsync_BankExpenseMovedOntoACard_CreatesTheStatement()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest()));

        await _sut.UpdateExpenseAsync(added.Id, ToUpdateDto(_repository, CardExpenseRequest(new DateOnly(2026, 10, 1))));

        StatementMonthsOf("BaAmex").Should().Equal((2026, 10));
    }

    [Fact]
    public async Task UpdateExpenseAsync_InvoiceMonthChanged_CreatesTheStatementOfTheNewMonth()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, CardExpenseRequest(new DateOnly(2026, 9, 1))));

        await _sut.UpdateExpenseAsync(added.Id, ToUpdateDto(_repository, CardExpenseRequest(new DateOnly(2026, 10, 1))));
        await _sut.UpdateExpenseAsync(added.Id, ToUpdateDto(_repository, CardExpenseRequest(new DateOnly(2026, 10, 1)) with { Value = 7m }));

        StatementMonthsOf("BaAmex").Should().Equal((2026, 9), (2026, 10));
    }

    [Fact]
    public async Task UpdateExpenseAsync_DescriptionOfAChargeOnAPaidStatement_ReturnsStatementPaidStatus()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest() with { PaymentSource = null, CardTag = "ChaseMaster4023" }));
        MarkStatementPaid("ChaseMaster4023", 2026, 7);

        var result = await _sut.UpdateExpenseAsync(added.Id, ToUpdateDto(_repository, ValidCreateRequest() with
        {
            Description = "Renamed",
            PaymentSource = null,
            CardTag = "ChaseMaster4023",
            InvoiceDate = new DateOnly(2026, 7, 1)
        }));

        using (new AssertionScope())
        {
            result.Description.Should().Be("Renamed");
            result.PaymentStatus.Should().Be("CreditCardStatementPaid");
        }
    }

    [Fact]
    public async Task UpdateExpenseAsync_LegacySettledChargeEditedFromMonthly_KeepsItsChargeDate()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest() with { PaymentSource = null, CardTag = "ChaseMaster4023" }));
        var charge = _repository.Expenses.Single(e => e.Id == added.Id);
        charge.Settle(_repository.Accounts.Single(a => a.Name == "Barclays"), new DateOnly(2026, 8, 4));
        MarkStatementPaid("ChaseMaster4023", 2026, 7);

        var result = await _sut.UpdateExpenseAsync(added.Id, ToUpdateDto(_repository, ValidCreateRequest() with
        {
            Date = new DateOnly(2026, 8, 4),
            Description = "Renamed",
            PaymentSource = "Barclays",
            CardTag = "ChaseMaster4023"
        }));

        using (new AssertionScope())
        {
            result.Description.Should().Be("Renamed");
            result.PaymentStatus.Should().Be("CreditCardSettled");
            charge.ChargeDate.Should().Be(new DateOnly(2026, 7, 15));
            charge.Date.Should().Be(new DateOnly(2026, 8, 4));
            charge.InvoiceDate.Should().Be(new DateOnly(2026, 7, 1));
        }
    }

    private const string LaterUnpaidStatementMessage = "Charges can only move to a later, unpaid statement.";
    private const string InvalidStatementMonthMessage = "The statement month is not a valid month.";

    private Task<ExpenseDTO> MoveAsync(Guid id, int year, int month) =>
        _sut.MoveToStatementAsync(id, new MoveChargeToStatementDTO { Year = year, Month = month });

    [Fact]
    public async Task MoveToStatementAsync_UnpaidCharge_MovesToTheLaterStatementAndCreatesIt()
    {
        var card = _repository.CardAccounts.Single(c => c.Name == "BaAmex");
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, CardExpenseRequest(new DateOnly(2026, 9, 1))));
        _calendarSyncTrigger.InvoiceChecks.Clear();
        var savesBefore = _repository.SaveChangesCallCount;

        var result = await MoveAsync(added.Id, 2026, 12);

        using (new AssertionScope())
        {
            result.InvoiceDate.Should().Be(new DateOnly(2026, 12, 1));
            _repository.Expenses.Single().InvoiceDate.Should().Be(new DateOnly(2026, 12, 1));
            _repository.CardStatements.Should().ContainSingle(s => s.IsFor(card.Id, 2026, 12));
            _repository.SaveChangesCallCount.Should().Be(savesBefore + 1);
            _calendarSyncTrigger.InvoiceChecks.Should().Equal((card.Id, new DateOnly(2026, 9, 1)), (card.Id, new DateOnly(2026, 12, 1)));
            _tracer.Spans.Should().Contain(s => s.Name == "CashFlow.ExpenseService.MoveToStatement"
                && (string?)s.Attributes[TelemetryAttributeKeys.OperationResult] == TelemetryOperationResults.Success);
        }
    }

    [Theory]
    [InlineData(2026, 9, LaterUnpaidStatementMessage)]
    [InlineData(2026, 8, LaterUnpaidStatementMessage)]
    [InlineData(2026, 13, InvalidStatementMonthMessage)]
    [InlineData(0, 10, InvalidStatementMonthMessage)]
    public async Task MoveToStatementAsync_TargetNotLaterOrInvalid_ThrowsAndChangesNothing(int year, int month, string message)
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, CardExpenseRequest(new DateOnly(2026, 9, 1))));
        var savesBefore = _repository.SaveChangesCallCount;

        var act = async () => await MoveAsync(added.Id, year, month);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage(message);
        using (new AssertionScope())
        {
            _repository.Expenses.Single().InvoiceDate.Should().Be(new DateOnly(2026, 9, 1));
            _repository.SaveChangesCallCount.Should().Be(savesBefore);
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ExpenseService.MoveToStatement");
        }
    }

    [Fact]
    public async Task MoveToStatementAsync_TargetStatementPaid_Throws()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, CardExpenseRequest(new DateOnly(2026, 9, 1))));
        MarkStatementPaid("BaAmex", 2026, 10);

        var act = async () => await MoveAsync(added.Id, 2026, 10);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage(LaterUnpaidStatementMessage);
        _repository.Expenses.Single().InvoiceDate.Should().Be(new DateOnly(2026, 9, 1));
        _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ExpenseService.MoveToStatement");
    }

    [Fact]
    public async Task MoveToStatementAsync_OtherCardsPaidStatementInTheTargetMonth_Moves()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, CardExpenseRequest(new DateOnly(2026, 9, 1))));
        MarkStatementPaid("BarclaysPlatinumVisa8003", 2026, 10);

        var result = await MoveAsync(added.Id, 2026, 10);

        result.InvoiceDate.Should().Be(new DateOnly(2026, 10, 1));
    }

    [Fact]
    public async Task MoveToStatementAsync_ChargeOnAPaidStatement_ThrowsTheLockMessage()
    {
        var added = await AddChargeOnPaidStatementAsync();

        var act = async () => await MoveAsync(added.Id, 2026, 10);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage(PaidStatementMessage);
        _repository.Expenses.Single().InvoiceDate.Should().Be(new DateOnly(2026, 9, 1));
    }

    [Fact]
    public async Task MoveToStatementAsync_BankExpense_Throws()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest()));

        var act = async () => await MoveAsync(added.Id, 2026, 12);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Only an unpaid card charge can move to another statement.");
    }

    [Fact]
    public async Task MoveToStatementAsync_InactiveCard_Throws()
    {
        var added = await _sut.AddExpenseAsync(ToCreateDto(_repository, CardExpenseRequest(new DateOnly(2026, 9, 1))));
        _repository.CardAccounts.Single(c => c.Name == "BaAmex").SetActive(false);

        var act = async () => await MoveAsync(added.Id, 2026, 12);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Credit card 'BaAmex' is inactive and cannot be used for new entries.");
        _repository.Expenses.Single().InvoiceDate.Should().Be(new DateOnly(2026, 9, 1));
        _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ExpenseService.MoveToStatement");
    }

    [Fact]
    public async Task MoveToStatementAsync_UnknownExpense_ThrowsNotFound()
    {
        var act = async () => await MoveAsync(Guid.NewGuid(), 2026, 12);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
