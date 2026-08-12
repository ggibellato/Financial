using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Application.Tests.TestHelpers;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;
using CreditCard = Financial.CashFlow.Domain.Entities.CreditCard;

namespace Financial.CashFlow.Application.Tests.Services;

public class ExpenseServiceTests
{
    private static readonly Bank ChaseFixture = Bank.Create("Chase", roundUpEnabled: true);
    private static readonly CreditCard BarclaysPlatinumVisa8003Fixture = CreditCard.Create("BarclaysPlatinumVisa8003");
    private static readonly CreditCard BaAmexFixture = CreditCard.Create("BaAmex");

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new ExpenseService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public async Task AddExpenseAsync_WithValidRequest_SavesAndReturnsExpense()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest());

        var result = await service.AddExpenseAsync(request);

        using (new AssertionScope())
        {
            result.Description.Should().Be("Weekly groceries");
            result.Value.Should().Be(54.32m);
            result.Category.Should().Be("Mercado");
            result.PaymentSourceBankName.Should().Be("Barclays");
            result.CreditCardName.Should().BeNull();
            result.PaymentStatus.Should().Be("ImmediatePayment");
            repository.Expenses.Should().ContainSingle();
            repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task AddExpenseAsync_WithCardTagAndNoPaymentSource_SavesAsCreditCardCharge()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ValidCreateRequest() with { PaymentSource = null, CardTag = "BarclaysPlatinumVisa8003" };

        var result = await service.AddExpenseAsync(ToCreateDto(repository, request));

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
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { PaymentSource = null, CardTag = null });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*payment source or a card tag*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithBothPaymentSourceAndCardTag_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { CardTag = "BarclaysPlatinumVisa8003" });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*marking its card statement paid*");
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithBothPaymentSourceAndCardTag_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var added = await service.AddExpenseAsync(ToCreateDto(repository, ValidCreateRequest()));
        var updateRequest = ToUpdateDto(repository, ValidCreateRequest() with { CardTag = "BaAmex" });

        var act = async () => await service.UpdateExpenseAsync(added.Id, updateRequest);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*marking its card statement paid*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithZeroValue_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { Value = 0m });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*zero*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithMissingCategory_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { Category = "NotACategory" });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Category*not recognized*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithInvalidPaymentSource_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { PaymentSource = "NotASource" });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Payment source*not recognized*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithInvalidCardTag_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { CardTag = "NotACard" });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Credit card*not recognized*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithInactiveCard_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var inactiveCard = CreditCard.Create("RetiredCard", isActive: false);
        repository.CreditCards.Add(inactiveCard);
        var service = new ExpenseService(repository);
        var request = new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 15),
            Description = "Weekly groceries",
            Value = 54.32m,
            Category = "Mercado",
            CreditCardId = inactiveCard.Id
        };

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*RetiredCard*inactive*cannot be used for new entries*");
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithInactiveCard_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var added = await service.AddExpenseAsync(ToCreateDto(repository, ValidCreateRequest()));
        var inactiveCard = CreditCard.Create("RetiredCard", isActive: false);
        repository.CreditCards.Add(inactiveCard);
        var updateRequest = new ExpenseUpdateDTO
        {
            Date = new DateOnly(2026, 7, 15),
            Description = "Weekly groceries",
            Value = 54.32m,
            Category = "Mercado",
            CreditCardId = inactiveCard.Id
        };

        var act = async () => await service.UpdateExpenseAsync(added.Id, updateRequest);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*RetiredCard*inactive*cannot be used for new entries*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithBlankDescription_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { Description = "  " });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Description is required*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithDescriptionOver200Characters_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { Description = new string('a', 201) });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*200 characters*");
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithInvalidPaymentSource_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var added = await service.AddExpenseAsync(ToCreateDto(repository, ValidCreateRequest()));
        var updateRequest = ToUpdateDto(repository, ValidCreateRequest() with { PaymentSource = "NotASource" });

        var act = async () => await service.UpdateExpenseAsync(added.Id, updateRequest);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Payment source*not recognized*");
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithExistingId_UpdatesInPlace()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var added = await service.AddExpenseAsync(ToCreateDto(repository, ValidCreateRequest()));

        var updateRequest = new ExpenseUpdateDTO
        {
            Date = new DateOnly(2026, 8, 1),
            Description = "Updated",
            Value = 10m,
            Category = "Casa",
            PaymentSourceBankId = repository.Banks.First(b => b.Name == "Chase").Id,
            CreditCardId = null
        };
        var result = await service.UpdateExpenseAsync(added.Id, updateRequest);

        using (new AssertionScope())
        {
            result.Id.Should().Be(added.Id);
            result.Description.Should().Be("Updated");
            result.Category.Should().Be("Casa");
            repository.Expenses.Should().ContainSingle();
            repository.SaveChangesCallCount.Should().Be(2);
        }
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var updateRequest = ToUpdateDto(repository, ValidCreateRequest());

        var act = async () => await service.UpdateExpenseAsync(Guid.NewGuid(), updateRequest);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteExpenseAsync_WithExistingId_RemovesAndSaves()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var added = await service.AddExpenseAsync(ToCreateDto(repository, ValidCreateRequest()));

        await service.DeleteExpenseAsync(added.Id);

        repository.Expenses.Should().BeEmpty();
        repository.SaveChangesCallCount.Should().Be(2);
    }

    [Fact]
    public async Task DeleteExpenseAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);

        var act = async () => await service.DeleteExpenseAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetExpensesByMonth_ReturnsOnlyExpensesInThatMonth()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        await service.AddExpenseAsync(ToCreateDto(repository, ValidCreateRequest() with { Date = new DateOnly(2026, 7, 10) }));
        await service.AddExpenseAsync(ToCreateDto(repository, ValidCreateRequest() with { Date = new DateOnly(2026, 8, 10) }));

        var result = service.GetExpensesByMonth(2026, 7);

        result.Should().ContainSingle().Which.Date.Should().Be(new DateOnly(2026, 7, 10));
    }

    [Fact]
    public async Task GetExpensesByMonth_OrdersByDateDescending()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        await service.AddExpenseAsync(ToCreateDto(repository, ValidCreateRequest() with { Date = new DateOnly(2026, 7, 10) }));
        await service.AddExpenseAsync(ToCreateDto(repository, ValidCreateRequest() with { Date = new DateOnly(2026, 7, 25) }));
        await service.AddExpenseAsync(ToCreateDto(repository, ValidCreateRequest() with { Date = new DateOnly(2026, 7, 1) }));

        var result = service.GetExpensesByMonth(2026, 7);

        result.Select(e => e.Date).Should().Equal(
            new DateOnly(2026, 7, 25),
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 1));
    }

    [Fact]
    public async Task GetExpensesByMonth_SettledCardExpense_KeepsInvoiceDatePositionAfterSettlement()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var bankExpense = await service.AddExpenseAsync(ToCreateDto(repository, 
            ValidCreateRequest() with { Description = "Bank", Date = new DateOnly(2026, 7, 15) }));
        var cardExpense = await service.AddExpenseAsync(ToCreateDto(repository, 
            ValidCreateRequest() with { Description = "Card", Date = new DateOnly(2026, 7, 10), PaymentSource = null, CardTag = "BaAmex" }));
        // Settled after the bank expense's date, with a payment date in a later month entirely -
        // under a Date-based sort this would drop the card expense out of July's view; sorting by
        // InvoiceDate keeps it anchored to the invoice period it was assigned to.
        repository.Expenses.Single(e => e.Id == cardExpense.Id).Settle(ChaseFixture, new DateOnly(2026, 8, 25));

        var result = service.GetExpensesByMonth(2026, 7);

        result.Select(e => e.Id).Should().Equal(bankExpense.Id, cardExpense.Id);
    }

    [Fact]
    public async Task GetExpensesByMonth_SettledCardExpense_UsesInvoiceDateMonthNotChargeDateMonth()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var cardExpense = await service.AddExpenseAsync(ToCreateDto(repository, ValidCreateRequest() with
        {
            Description = "Card", Date = new DateOnly(2026, 8, 6), PaymentSource = null, CardTag = "BaAmex",
            InvoiceDate = new DateOnly(2026, 9, 1),
        }));
        repository.Expenses.Single(e => e.Id == cardExpense.Id).Settle(ChaseFixture, new DateOnly(2026, 9, 20));

        var augustResult = service.GetExpensesByMonth(2026, 8);
        var septemberResult = service.GetExpensesByMonth(2026, 9);

        using (new AssertionScope())
        {
            augustResult.Should().BeEmpty();
            septemberResult.Should().ContainSingle().Which.Id.Should().Be(cardExpense.Id);
        }
    }

    [Fact]
    public async Task GetExpensesByMonth_UnsettledCreditCardCharge_IsExcluded()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        await service.AddExpenseAsync(ToCreateDto(repository, 
            ValidCreateRequest() with { PaymentSource = null, CardTag = "ChaseMaster4023" }));

        var result = service.GetExpensesByMonth(2026, 7);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExpensesByMonth_ImmediatePayment_IsIncluded()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        await service.AddExpenseAsync(ToCreateDto(repository, ValidCreateRequest()));

        var result = service.GetExpensesByMonth(2026, 7);

        result.Should().ContainSingle().Which.PaymentStatus.Should().Be("ImmediatePayment");
    }

    [Fact]
    public async Task GetExpensesByMonth_MixOfStatuses_OnlyExcludesUnsettledCharge()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var immediate = await service.AddExpenseAsync(
            ToCreateDto(repository, ValidCreateRequest() with { Description = "Immediate" }));
        await service.AddExpenseAsync(ToCreateDto(repository, 
            ValidCreateRequest() with { Description = "Unsettled charge", PaymentSource = null, CardTag = "ChaseMaster4023" }));
        var settledCharge = await service.AddExpenseAsync(ToCreateDto(repository, 
            ValidCreateRequest() with { Description = "Settled charge", PaymentSource = null, CardTag = "BaAmex" }));
        repository.Expenses.Single(e => e.Id == settledCharge.Id).Settle(ChaseFixture, new DateOnly(2026, 7, 20));

        var result = service.GetExpensesByMonth(2026, 7);

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
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        await service.AddExpenseAsync(ToCreateDto(repository, 
            ValidCreateRequest() with { PaymentSource = null, CardTag = "ChaseMaster4023" }));

        var result = service.GetUnpaidCardChargesByMonth(2026, 7);

        result.Should().ContainSingle().Which.PaymentStatus.Should().Be("CreditCardCharge");
    }

    [Fact]
    public async Task GetUnpaidCardChargesByMonth_ImmediatePaymentAndSettledCharge_AreExcluded()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        await service.AddExpenseAsync(ToCreateDto(repository, ValidCreateRequest() with { Description = "Immediate" }));
        var settledCharge = await service.AddExpenseAsync(ToCreateDto(repository, 
            ValidCreateRequest() with { Description = "Settled charge", PaymentSource = null, CardTag = "BaAmex" }));
        repository.Expenses.Single(e => e.Id == settledCharge.Id).Settle(ChaseFixture, new DateOnly(2026, 7, 20));

        var result = service.GetUnpaidCardChargesByMonth(2026, 7);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUnpaidCardChargesByMonth_OutsideMonth_IsExcluded()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        await service.AddExpenseAsync(ToCreateDto(repository, ValidCreateRequest() with
        {
            Date = new DateOnly(2026, 8, 10), PaymentSource = null, CardTag = "ChaseMaster4023"
        }));

        var result = service.GetUnpaidCardChargesByMonth(2026, 7);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUnpaidCardChargesByMonth_InvoiceDateInDifferentMonthThanChargeDate_AppearsUnderInvoiceMonth()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var charge = await service.AddExpenseAsync(ToCreateDto(repository, ValidCreateRequest() with
        {
            Date = new DateOnly(2026, 8, 6), PaymentSource = null, CardTag = "ChaseMaster4023",
            InvoiceDate = new DateOnly(2026, 9, 1),
        }));

        var augustResult = service.GetUnpaidCardChargesByMonth(2026, 8);
        var septemberResult = service.GetUnpaidCardChargesByMonth(2026, 9);

        using (new AssertionScope())
        {
            augustResult.Should().BeEmpty();
            septemberResult.Should().ContainSingle().Which.Id.Should().Be(charge.Id);
        }
    }

    [Fact]
    public async Task GetCategoryTotalsByMonth_SumsValuesPerCategoryForThatMonth()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        await service.AddExpenseAsync(ToCreateDto(repository, ValidCreateRequest() with { Category = "Mercado", Value = 10m }));
        await service.AddExpenseAsync(ToCreateDto(repository, ValidCreateRequest() with { Category = "Mercado", Value = 5m }));
        await service.AddExpenseAsync(ToCreateDto(repository, ValidCreateRequest() with { Category = "Casa", Value = 20m }));

        var result = service.GetCategoryTotalsByMonth(2026, 7);

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
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        await service.AddExpenseAsync(ToCreateDto(repository, ValidCreateRequest() with { Category = "Reserva", Value = 100m }));
        await service.AddExpenseAsync(ToCreateDto(repository, ValidCreateRequest() with { Category = "Reserva", Value = -30m }));

        var result = service.GetCategoryTotalsByMonth(2026, 7);

        result.Should().ContainSingle(t => t.Category == "Reserva" && t.TotalValue == 70m);
    }

    [Fact]
    public void GetCategoryTotalsByMonth_UnpaidCardCharge_CountsTowardInvoiceMonthNotChargeMonth()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var charge = Expense.Create(
            new DateOnly(2026, 7, 29), "Cutoff charge", 40m, Category.Mercado, null,
           BarclaysPlatinumVisa8003Fixture, new DateOnly(2026, 8, 1));
        repository.Expenses.Add(charge);
        var service = new ExpenseService(repository);

        var julyResult = service.GetCategoryTotalsByMonth(2026, 7);
        var augustResult = service.GetCategoryTotalsByMonth(2026, 8);

        using (new AssertionScope())
        {
            julyResult.Should().BeEmpty();
            augustResult.Should().ContainSingle(t => t.Category == "Mercado" && t.TotalValue == 40m);
        }
    }

    [Fact]
    public void GetCategoryTotalsByMonth_SettledCardCharge_CountsTowardPostSettlementDateMonth()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var charge = Expense.Create(new DateOnly(2026, 7, 10), "Settled charge", 40m, Category.Mercado, null, BarclaysPlatinumVisa8003Fixture);
        charge.Settle(ChaseFixture, new DateOnly(2026, 8, 3));
        repository.Expenses.Add(charge);
        var service = new ExpenseService(repository);

        var julyResult = service.GetCategoryTotalsByMonth(2026, 7);
        var augustResult = service.GetCategoryTotalsByMonth(2026, 8);

        using (new AssertionScope())
        {
            julyResult.Should().BeEmpty();
            augustResult.Should().ContainSingle(t => t.Category == "Mercado" && t.TotalValue == 40m);
        }
    }

    [Fact]
    public void GetCategoryTotalsByMonth_MixOfUnpaidSettledAndBank_NoExpenseCountedInMoreThanOneMonth()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var unpaidCutoff = Expense.Create(
            new DateOnly(2026, 7, 29), "Unpaid cutoff", 10m, Category.Mercado, null,
           BarclaysPlatinumVisa8003Fixture, new DateOnly(2026, 8, 1));
        var settled = Expense.Create(new DateOnly(2026, 7, 12), "Settled", 20m, Category.Mercado, null, BaAmexFixture);
        settled.Settle(ChaseFixture, new DateOnly(2026, 7, 20));
        var bank = Expense.Create(new DateOnly(2026, 7, 15), "Bank", 30m, Category.Mercado, ChaseFixture, null);
        repository.Expenses.Add(unpaidCutoff);
        repository.Expenses.Add(settled);
        repository.Expenses.Add(bank);
        var service = new ExpenseService(repository);

        var julyTotal = service.GetCategoryTotalsByMonth(2026, 7).Sum(t => t.TotalValue);
        var augustTotal = service.GetCategoryTotalsByMonth(2026, 8).Sum(t => t.TotalValue);

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
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { PaymentSource = "Trading212", Value = 9.40m, RoundUpAmount = 0.60m });

        var result = await service.AddExpenseAsync(request);

        result.RoundUpAmount.Should().Be(0.60m);
    }

    [Fact]
    public async Task AddExpenseAsync_WithRoundUpAmountOfZero_SavesExplicitZero()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { PaymentSource = "Trading212", Value = 10.00m, RoundUpAmount = 0.00m });

        var result = await service.AddExpenseAsync(request);

        result.RoundUpAmount.Should().Be(0.00m);
    }

    [Fact]
    public async Task AddExpenseAsync_WithRoundUpAmountOnNonRoundUpBank_ThrowsNamingTheBank()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { PaymentSource = "Barclays", RoundUpAmount = 0.50m });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Barclays*does not support round-up*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithRoundUpAmountOnCreditCardTaggedExpense_Throws()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { PaymentSource = null, CardTag = "ChaseMaster4023", RoundUpAmount = 0.50m });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*not a credit-card charge*");
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.00)]
    public async Task AddExpenseAsync_WithRoundUpAmountOutsideRange_Throws(decimal roundUpAmount)
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { PaymentSource = "Chase", RoundUpAmount = roundUpAmount });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*between £0.00 and £0.99*");
    }

    [Fact]
    public async Task AddExpenseAsync_EligibleWithNoRoundUpAmount_ReturnsSuggestedAmount()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { PaymentSource = "Trading212", Value = 9.40m });

        var result = await service.AddExpenseAsync(request);

        result.RoundUpAmount.Should().BeNull();
        result.SuggestedRoundUpAmount.Should().Be(0.60m);
    }

    [Fact]
    public async Task AddExpenseAsync_EligibleWithRoundUpAmountAlreadySaved_ReturnsNoSuggestion()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { PaymentSource = "Trading212", Value = 9.40m, RoundUpAmount = 0.60m });

        var result = await service.AddExpenseAsync(request);

        result.SuggestedRoundUpAmount.Should().BeNull();
    }

    [Fact]
    public async Task AddExpenseAsync_OnNonRoundUpBank_ReturnsNoSuggestion()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { PaymentSource = "Barclays", Value = 9.40m });

        var result = await service.AddExpenseAsync(request);

        result.SuggestedRoundUpAmount.Should().BeNull();
    }

    [Fact]
    public async Task AddExpenseAsync_CreditCardCharge_ReturnsNoSuggestion()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { PaymentSource = null, CardTag = "ChaseMaster4023", Value = 9.40m });

        var result = await service.AddExpenseAsync(request);

        result.SuggestedRoundUpAmount.Should().BeNull();
    }

    [Fact]
    public async Task AddExpenseAsync_NegativeValueOnRoundUpBank_ReturnsNoSuggestion()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { PaymentSource = "Trading212", Value = -9.40m });

        var result = await service.AddExpenseAsync(request);

        result.SuggestedRoundUpAmount.Should().BeNull();
    }

    [Fact]
    public async Task AddExpenseAsync_WithRoundUpAmountOnNegativeValue_Throws()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { PaymentSource = "Trading212", Value = -9.40m, RoundUpAmount = 0.60m });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*negative (reimbursement) expense*");
    }

    [Fact]
    public async Task UpdateExpenseAsync_ChangingValueOnly_LeavesRoundUpAmountUnchanged()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var added = await service.AddExpenseAsync(ToCreateDto(repository, 
            ValidCreateRequest() with { PaymentSource = "Trading212", Value = 9.40m, RoundUpAmount = 0.60m }));

        var updateRequest = ToUpdateDto(repository, 
            ValidCreateRequest() with { PaymentSource = "Trading212", Value = 20m, RoundUpAmount = 0.60m });
        var result = await service.UpdateExpenseAsync(added.Id, updateRequest);

        result.Value.Should().Be(20m);
        result.RoundUpAmount.Should().Be(0.60m);
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithNewRoundUpAmount_ChangesIt()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var added = await service.AddExpenseAsync(ToCreateDto(repository, 
            ValidCreateRequest() with { PaymentSource = "Trading212", RoundUpAmount = 0.60m }));

        var updateRequest = ToUpdateDto(repository, ValidCreateRequest() with { PaymentSource = "Trading212", RoundUpAmount = 0.10m });
        var result = await service.UpdateExpenseAsync(added.Id, updateRequest);

        result.RoundUpAmount.Should().Be(0.10m);
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithNullRoundUpAmount_ClearsAPreviouslySavedAmount()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var added = await service.AddExpenseAsync(ToCreateDto(repository, 
            ValidCreateRequest() with { PaymentSource = "Trading212", RoundUpAmount = 0.60m }));

        var updateRequest = ToUpdateDto(repository, ValidCreateRequest() with { PaymentSource = "Trading212", RoundUpAmount = null });
        var result = await service.UpdateExpenseAsync(added.Id, updateRequest);

        result.RoundUpAmount.Should().BeNull();
    }

    private static ExpenseCreateRequest ValidCreateRequest() => new(
        new DateOnly(2026, 7, 15),
        "Weekly groceries",
        54.32m,
        "Mercado",
        "Barclays",
        null);

    private static ExpenseCreateDTO ToCreateDto(StubCashFlowRepository repository, ExpenseCreateRequest r) => new()
    {
        Date = r.Date,
        Description = r.Description,
        Value = r.Value,
        Category = r.Category,
        PaymentSourceBankId = ResolveBankId(repository, r.PaymentSource),
        CreditCardId = ResolveCreditCardId(repository, r.CardTag),
        InvoiceDate = r.InvoiceDate,
        RoundUpAmount = r.RoundUpAmount
    };

    private static ExpenseUpdateDTO ToUpdateDto(StubCashFlowRepository repository, ExpenseCreateRequest r) => new()
    {
        Date = r.Date,
        Description = r.Description,
        Value = r.Value,
        Category = r.Category,
        PaymentSourceBankId = ResolveBankId(repository, r.PaymentSource),
        CreditCardId = ResolveCreditCardId(repository, r.CardTag),
        InvoiceDate = r.InvoiceDate,
        RoundUpAmount = r.RoundUpAmount
    };

    /// <summary>An unresolvable name maps to a random, never-seeded Guid so tests exercising an unrecognized reference still hit the "not found" path rather than the "omitted" path.</summary>
    private static Guid? ResolveBankId(StubCashFlowRepository repository, string? bankName) =>
        bankName is null ? null : repository.Banks.FirstOrDefault(b => b.Name == bankName)?.Id ?? Guid.NewGuid();

    /// <summary>An unresolvable name maps to a random, never-seeded Guid so tests exercising an unrecognized reference still hit the "not found" path rather than the "omitted" path.</summary>
    private static Guid? ResolveCreditCardId(StubCashFlowRepository repository, string? cardName) =>
        cardName is null ? null : repository.CreditCards.FirstOrDefault(c => c.Name == cardName)?.Id ?? Guid.NewGuid();

    private sealed record ExpenseCreateRequest(
        DateOnly Date, string Description, decimal Value, string Category, string? PaymentSource, string? CardTag,
        decimal? RoundUpAmount = null, DateOnly? InvoiceDate = null);

    [Fact]
    public async Task AddExpenseAsync_CreditCardExpense_ReturnsNonNullChargeDateAndInvoiceDate()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { PaymentSource = null, CardTag = "ChaseMaster4023" });

        var result = await service.AddExpenseAsync(request);

        using (new AssertionScope())
        {
            result.ChargeDate.Should().NotBeNull();
            result.InvoiceDate.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task AddExpenseAsync_WithInvoiceDateOverride_UsesProvidedMonth()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with
        {
            Date = new DateOnly(2026, 7, 29),
            PaymentSource = null,
            CardTag = "ChaseMaster4023",
            InvoiceDate = new DateOnly(2026, 8, 17)
        });

        var result = await service.AddExpenseAsync(request);

        result.InvoiceDate.Should().Be(new DateOnly(2026, 8, 1));
    }

    [Fact]
    public async Task AddExpenseAsync_WithoutInvoiceDateOverride_DefaultsToChargeMonth()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with
        {
            Date = new DateOnly(2026, 7, 15), PaymentSource = null, CardTag = "ChaseMaster4023"
        });

        var result = await service.AddExpenseAsync(request);

        result.InvoiceDate.Should().Be(new DateOnly(2026, 7, 1));
    }

    [Fact]
    public async Task AddExpenseAsync_BankExpense_ChargeDateAndInvoiceDateAreNull()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);

        var result = await service.AddExpenseAsync(ToCreateDto(repository, ValidCreateRequest()));

        using (new AssertionScope())
        {
            result.ChargeDate.Should().BeNull();
            result.InvoiceDate.Should().BeNull();
        }
    }

    [Fact]
    public async Task UpdateExpenseAsync_ChangingInvoiceDateWhileUnpaid_PersistsOverride()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var added = await service.AddExpenseAsync(ToCreateDto(repository, 
            ValidCreateRequest() with { PaymentSource = null, CardTag = "ChaseMaster4023" }));
        var updateRequest = ToUpdateDto(repository, ValidCreateRequest() with
        {
            PaymentSource = null, CardTag = "ChaseMaster4023", InvoiceDate = new DateOnly(2026, 8, 12)
        });

        var result = await service.UpdateExpenseAsync(added.Id, updateRequest);

        result.InvoiceDate.Should().Be(new DateOnly(2026, 8, 1));
    }

    [Fact]
    public async Task UpdateExpenseAsync_EchoingUnchangedInvoiceDateOnSettledExpense_Succeeds()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var added = await service.AddExpenseAsync(ToCreateDto(repository, 
            ValidCreateRequest() with { PaymentSource = null, CardTag = "ChaseMaster4023" }));
        repository.Expenses.Single(e => e.Id == added.Id).Settle(repository.Banks.First(b => b.Name == "Barclays"), new DateOnly(2026, 7, 31));
        var updateRequest = ToUpdateDto(repository, ValidCreateRequest() with
        {
            Description = "Renamed", PaymentSource = "Barclays", CardTag = "ChaseMaster4023", InvoiceDate = added.InvoiceDate
        });

        var result = await service.UpdateExpenseAsync(added.Id, updateRequest);

        result.Description.Should().Be("Renamed");
    }

    [Fact]
    public async Task UpdateExpenseAsync_ChangingInvoiceDateOnSettledExpense_Throws()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultCreditCards: true);
        var service = new ExpenseService(repository);
        var added = await service.AddExpenseAsync(ToCreateDto(repository, 
            ValidCreateRequest() with { PaymentSource = null, CardTag = "ChaseMaster4023" }));
        repository.Expenses.Single(e => e.Id == added.Id).Settle(repository.Banks.First(b => b.Name == "Barclays"), new DateOnly(2026, 7, 31));
        var updateRequest = ToUpdateDto(repository, ValidCreateRequest() with
        {
            PaymentSource = "Barclays", CardTag = "ChaseMaster4023", InvoiceDate = new DateOnly(2026, 9, 1)
        });

        var act = async () => await service.UpdateExpenseAsync(added.Id, updateRequest);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*unsettled credit card charge*");
    }
}
