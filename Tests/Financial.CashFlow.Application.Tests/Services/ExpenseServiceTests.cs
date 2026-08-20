using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;
using CreditCard = Financial.CashFlow.Domain.Entities.CreditCard;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class ExpenseServiceTests
{
    private static readonly Bank ChaseFixture = Bank.Create("Chase", roundUpEnabled: true);
    private static readonly CreditCard BarclaysPlatinumVisa8003Fixture = CreditCard.Create("BarclaysPlatinumVisa8003");
    private static readonly CreditCard BaAmexFixture = CreditCard.Create("BaAmex");
    private static readonly Category MercadoFixture = Category.Create("Mercado");
    private static readonly Microsoft.Extensions.Logging.ILogger<ExpenseService> Logger = NullLogger<ExpenseService>.Instance;

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly ExpenseService _sut;

    public ExpenseServiceTests()
    {
        _repository = CreateRepository();
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    /// <summary>The seeding nearly every test needs; the flags let the few tests that must start without
    /// a seeded credit card or category opt out without repeating the whole construction sequence.</summary>
    private static StubCashFlowRepository CreateRepository(
        bool seedDefaultCreditCards = true, bool seedDefaultCategories = true) =>
        new(seedDefaultBanks: true, seedDefaultCreditCards: seedDefaultCreditCards, seedDefaultCategories: seedDefaultCategories);

    /// <summary>Wires the SUT exactly as the test constructor does, letting a test swap in the single
    /// dependency it needs to differ on.</summary>
    private ExpenseService CreateService(
        StubCashFlowRepository? repository = null,
        Microsoft.Extensions.Logging.ILogger<ExpenseService>? logger = null) =>
        new(repository ?? _repository, _tracer, logger ?? Logger);

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new ExpenseService(null!, _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new ExpenseService(_repository, null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

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
        var span = _tracer.Spans.Should().ContainSingle().Which;
        span.Name.Should().Be("CashFlow.ExpenseService.AddExpense");
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Failed);
        span.RecordedException.Should().BeOfType<ArgumentException>();
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
        var inactiveCard = CreditCard.Create("RetiredCard", isActive: false);
        repository.CreditCards.Add(inactiveCard);
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
        var inactiveCard = CreditCard.Create("RetiredCard", isActive: false);
        _repository.CreditCards.Add(inactiveCard);
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
            PaymentSourceBankId = repository.Banks.First(b => b.Name == "Barclays").Id
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
            PaymentSourceBankId = _repository.Banks.First(b => b.Name == "Barclays").Id
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
            PaymentSourceBankId = _repository.Banks.First(b => b.Name == "Chase").Id,
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
    public async Task GetExpensesByMonth_ReturnsOnlyExpensesInThatMonth()
    {
        await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest() with { Date = new DateOnly(2026, 7, 10) }));
        await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest() with { Date = new DateOnly(2026, 8, 10) }));

        var result = _sut.GetExpensesByMonth(2026, 7);

        result.Should().ContainSingle().Which.Date.Should().Be(new DateOnly(2026, 7, 10));
    }

    [Fact]
    public async Task GetExpensesByMonth_OrdersByDateDescending()
    {
        await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest() with { Date = new DateOnly(2026, 7, 10) }));
        await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest() with { Date = new DateOnly(2026, 7, 25) }));
        await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest() with { Date = new DateOnly(2026, 7, 1) }));

        var result = _sut.GetExpensesByMonth(2026, 7);

        result.Select(e => e.Date).Should().Equal(
            new DateOnly(2026, 7, 25),
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 1));
    }

    [Fact]
    public async Task GetExpensesByMonth_SettledCardExpense_KeepsInvoiceDatePositionAfterSettlement()
    {
        var bankExpense = await _sut.AddExpenseAsync(ToCreateDto(_repository, 
            ValidCreateRequest() with { Description = "Bank", Date = new DateOnly(2026, 7, 15) }));
        var cardExpense = await _sut.AddExpenseAsync(ToCreateDto(_repository, 
            ValidCreateRequest() with { Description = "Card", Date = new DateOnly(2026, 7, 10), PaymentSource = null, CardTag = "BaAmex" }));
        // Settled after the bank expense's date, with a payment date in a later month entirely -
        // under a Date-based sort this would drop the card expense out of July's view; sorting by
        // InvoiceDate keeps it anchored to the invoice period it was assigned to.
        _repository.Expenses.Single(e => e.Id == cardExpense.Id).Settle(ChaseFixture, new DateOnly(2026, 8, 25));

        var result = _sut.GetExpensesByMonth(2026, 7);

        result.Select(e => e.Id).Should().Equal(bankExpense.Id, cardExpense.Id);
    }

    [Fact]
    public async Task GetExpensesByMonth_SettledCardExpense_UsesInvoiceDateMonthNotChargeDateMonth()
    {
        var cardExpense = await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest() with
        {
            Description = "Card", Date = new DateOnly(2026, 8, 6), PaymentSource = null, CardTag = "BaAmex",
            InvoiceDate = new DateOnly(2026, 9, 1),
        }));
        _repository.Expenses.Single(e => e.Id == cardExpense.Id).Settle(ChaseFixture, new DateOnly(2026, 9, 20));

        var augustResult = _sut.GetExpensesByMonth(2026, 8);
        var septemberResult = _sut.GetExpensesByMonth(2026, 9);

        using (new AssertionScope())
        {
            augustResult.Should().BeEmpty();
            septemberResult.Should().ContainSingle().Which.Id.Should().Be(cardExpense.Id);
        }
    }

    [Fact]
    public async Task GetExpensesByMonth_UnsettledCreditCardCharge_IsExcluded()
    {
        await _sut.AddExpenseAsync(ToCreateDto(_repository, 
            ValidCreateRequest() with { PaymentSource = null, CardTag = "ChaseMaster4023" }));

        var result = _sut.GetExpensesByMonth(2026, 7);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExpensesByMonth_ImmediatePayment_IsIncluded()
    {
        await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest()));

        var result = _sut.GetExpensesByMonth(2026, 7);

        result.Should().ContainSingle().Which.PaymentStatus.Should().Be("ImmediatePayment");
    }

    [Fact]
    public async Task GetExpensesByMonth_MixOfStatuses_OnlyExcludesUnsettledCharge()
    {
        var immediate = await _sut.AddExpenseAsync(
            ToCreateDto(_repository, ValidCreateRequest() with { Description = "Immediate" }));
        await _sut.AddExpenseAsync(ToCreateDto(_repository, 
            ValidCreateRequest() with { Description = "Unsettled charge", PaymentSource = null, CardTag = "ChaseMaster4023" }));
        var settledCharge = await _sut.AddExpenseAsync(ToCreateDto(_repository, 
            ValidCreateRequest() with { Description = "Settled charge", PaymentSource = null, CardTag = "BaAmex" }));
        _repository.Expenses.Single(e => e.Id == settledCharge.Id).Settle(ChaseFixture, new DateOnly(2026, 7, 20));

        var result = _sut.GetExpensesByMonth(2026, 7);

        using (new AssertionScope())
        {
            result.Should().HaveCount(2);
            result.Should().Contain(e => e.Id == immediate.Id);
            result.Should().Contain(e => e.Id == settledCharge.Id);
        }
    }

    [Fact]
    public async Task GetUnpaidCardChargesByMonth_UnsettledCharge_IsIncluded()
    {
        await _sut.AddExpenseAsync(ToCreateDto(_repository, 
            ValidCreateRequest() with { PaymentSource = null, CardTag = "ChaseMaster4023" }));

        var result = _sut.GetUnpaidCardChargesByMonth(2026, 7);

        result.Should().ContainSingle().Which.PaymentStatus.Should().Be("CreditCardCharge");
    }

    [Fact]
    public async Task GetUnpaidCardChargesByMonth_OrdersByChargeDateDescendingAcrossCards()
    {
        await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest() with
        {
            Description = "Chase early", Date = new DateOnly(2026, 7, 2), PaymentSource = null, CardTag = "ChaseMaster4023",
        }));
        await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest() with
        {
            Description = "BaAmex late", Date = new DateOnly(2026, 7, 20), PaymentSource = null, CardTag = "BaAmex",
        }));
        await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest() with
        {
            Description = "Chase mid", Date = new DateOnly(2026, 7, 10), PaymentSource = null, CardTag = "ChaseMaster4023",
        }));

        var result = _sut.GetUnpaidCardChargesByMonth(2026, 7);

        // All three share the same InvoiceDate (2026-07-01), so this proves the sort uses the
        // actual charge date rather than the (here, always-equal) invoice-period date.
        result.Select(e => e.Description).Should().Equal("BaAmex late", "Chase mid", "Chase early");
    }

    [Fact]
    public async Task GetUnpaidCardChargesByMonth_ImmediatePaymentAndSettledCharge_AreExcluded()
    {
        await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest() with { Description = "Immediate" }));
        var settledCharge = await _sut.AddExpenseAsync(ToCreateDto(_repository, 
            ValidCreateRequest() with { Description = "Settled charge", PaymentSource = null, CardTag = "BaAmex" }));
        _repository.Expenses.Single(e => e.Id == settledCharge.Id).Settle(ChaseFixture, new DateOnly(2026, 7, 20));

        var result = _sut.GetUnpaidCardChargesByMonth(2026, 7);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUnpaidCardChargesByMonth_OutsideMonth_IsExcluded()
    {
        await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest() with
        {
            Date = new DateOnly(2026, 8, 10), PaymentSource = null, CardTag = "ChaseMaster4023"
        }));

        var result = _sut.GetUnpaidCardChargesByMonth(2026, 7);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUnpaidCardChargesByMonth_InvoiceDateInDifferentMonthThanChargeDate_AppearsUnderInvoiceMonth()
    {
        var charge = await _sut.AddExpenseAsync(ToCreateDto(_repository, ValidCreateRequest() with
        {
            Date = new DateOnly(2026, 8, 6), PaymentSource = null, CardTag = "ChaseMaster4023",
            InvoiceDate = new DateOnly(2026, 9, 1),
        }));

        var augustResult = _sut.GetUnpaidCardChargesByMonth(2026, 8);
        var septemberResult = _sut.GetUnpaidCardChargesByMonth(2026, 9);

        using (new AssertionScope())
        {
            augustResult.Should().BeEmpty();
            septemberResult.Should().ContainSingle().Which.Id.Should().Be(charge.Id);
        }
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
    public void GetCategoryTotalsByMonth_SettledCardCharge_CountsTowardPostSettlementDateMonth()
    {
        var charge = Expense.Create(new DateOnly(2026, 7, 10), "Settled charge", 40m, MercadoFixture, null, BarclaysPlatinumVisa8003Fixture);
        charge.Settle(ChaseFixture, new DateOnly(2026, 8, 3));
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
        bankName is null ? null : _repository.Banks.FirstOrDefault(b => b.Name == bankName)?.Id ?? Guid.NewGuid();

    /// <summary>An unresolvable name maps to a random, never-seeded Guid so tests exercising an unrecognized reference still hit the "not found" path rather than the "omitted" path.</summary>
    private static Guid? ResolveCreditCardId(StubCashFlowRepository _repository, string? cardName) =>
        cardName is null ? null : _repository.CreditCards.FirstOrDefault(c => c.Name == cardName)?.Id ?? Guid.NewGuid();

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
        _repository.Expenses.Single(e => e.Id == added.Id).Settle(_repository.Banks.First(b => b.Name == "Barclays"), new DateOnly(2026, 7, 31));
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
        _repository.Expenses.Single(e => e.Id == added.Id).Settle(_repository.Banks.First(b => b.Name == "Barclays"), new DateOnly(2026, 7, 31));
        var updateRequest = ToUpdateDto(_repository, ValidCreateRequest() with
        {
            PaymentSource = "Barclays", CardTag = "ChaseMaster4023", InvoiceDate = new DateOnly(2026, 9, 1)
        });

        var act = async () => await _sut.UpdateExpenseAsync(added.Id, updateRequest);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*unsettled credit card charge*");
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new ExpenseService(_repository, _tracer, null!);

        act.Should().Throw<ArgumentNullException>();
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
}
