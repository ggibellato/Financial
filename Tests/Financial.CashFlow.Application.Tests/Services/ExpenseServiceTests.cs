using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Application.Tests.TestHelpers;
using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Application.Tests.Services;

public class ExpenseServiceTests
{
    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new ExpenseService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public async Task AddExpenseAsync_WithValidRequest_SavesAndReturnsExpense()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new ExpenseService(repository);
        var request = ToCreateDto(ValidCreateRequest());

        var result = await service.AddExpenseAsync(request);

        using (new AssertionScope())
        {
            result.Description.Should().Be("Weekly groceries");
            result.Value.Should().Be(54.32m);
            result.Category.Should().Be("Mercado");
            result.PaymentSource.Should().Be("Barclays");
            result.CardTag.Should().BeNull();
            result.SettledAt.Should().BeNull();
            result.PaymentStatus.Should().Be("ImmediatePayment");
            repository.Expenses.Should().ContainSingle();
            repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task AddExpenseAsync_WithCardTagAndNoPaymentSource_SavesAsCreditCardCharge()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new ExpenseService(repository);
        var request = ValidCreateRequest() with { PaymentSource = null, CardTag = "BarclaysPlatinumVisa8003" };

        var result = await service.AddExpenseAsync(ToCreateDto(request));

        using (new AssertionScope())
        {
            result.CardTag.Should().Be("BarclaysPlatinumVisa8003");
            result.PaymentSource.Should().BeNull();
            result.SettledAt.Should().BeNull();
            result.PaymentStatus.Should().Be("CreditCardCharge");
        }
    }

    [Fact]
    public async Task AddExpenseAsync_WithNeitherPaymentSourceNorCardTag_ThrowsArgumentException()
    {
        var service = new ExpenseService(new StubCashFlowRepository(seedDefaultBanks: true));
        var request = ToCreateDto(ValidCreateRequest() with { PaymentSource = null, CardTag = null });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*payment source or a card tag*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithBothPaymentSourceAndCardTag_ThrowsArgumentException()
    {
        var service = new ExpenseService(new StubCashFlowRepository(seedDefaultBanks: true));
        var request = ToCreateDto(ValidCreateRequest() with { CardTag = "BarclaysPlatinumVisa8003" });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*marking its card statement paid*");
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithBothPaymentSourceAndCardTag_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new ExpenseService(repository);
        var added = await service.AddExpenseAsync(ToCreateDto(ValidCreateRequest()));
        var updateRequest = ToUpdateDto(ValidCreateRequest() with { CardTag = "BaAmex" });

        var act = async () => await service.UpdateExpenseAsync(added.Id, updateRequest);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*marking its card statement paid*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithZeroValue_ThrowsArgumentException()
    {
        var service = new ExpenseService(new StubCashFlowRepository(seedDefaultBanks: true));
        var request = ToCreateDto(ValidCreateRequest() with { Value = 0m });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*zero*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithMissingCategory_ThrowsArgumentException()
    {
        var service = new ExpenseService(new StubCashFlowRepository(seedDefaultBanks: true));
        var request = ToCreateDto(ValidCreateRequest() with { Category = "NotACategory" });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Category*not recognized*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithInvalidPaymentSource_ThrowsArgumentException()
    {
        var service = new ExpenseService(new StubCashFlowRepository(seedDefaultBanks: true));
        var request = ToCreateDto(ValidCreateRequest() with { PaymentSource = "NotASource" });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Payment source*not recognized*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithInvalidCardTag_ThrowsArgumentException()
    {
        var service = new ExpenseService(new StubCashFlowRepository(seedDefaultBanks: true));
        var request = ToCreateDto(ValidCreateRequest() with { CardTag = "NotACard" });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Credit card*not recognized*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithBlankDescription_ThrowsArgumentException()
    {
        var service = new ExpenseService(new StubCashFlowRepository(seedDefaultBanks: true));
        var request = ToCreateDto(ValidCreateRequest() with { Description = "  " });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Description is required*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithDescriptionOver200Characters_ThrowsArgumentException()
    {
        var service = new ExpenseService(new StubCashFlowRepository(seedDefaultBanks: true));
        var request = ToCreateDto(ValidCreateRequest() with { Description = new string('a', 201) });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*200 characters*");
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithExistingId_UpdatesInPlace()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new ExpenseService(repository);
        var added = await service.AddExpenseAsync(ToCreateDto(ValidCreateRequest()));

        var updateRequest = new ExpenseUpdateDTO
        {
            Date = new DateOnly(2026, 8, 1),
            Description = "Updated",
            Value = 10m,
            Category = "Casa",
            PaymentSource = "Chase",
            CardTag = null
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
        var service = new ExpenseService(new StubCashFlowRepository(seedDefaultBanks: true));
        var updateRequest = ToUpdateDto(ValidCreateRequest());

        var act = async () => await service.UpdateExpenseAsync(Guid.NewGuid(), updateRequest);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteExpenseAsync_WithExistingId_RemovesAndSaves()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new ExpenseService(repository);
        var added = await service.AddExpenseAsync(ToCreateDto(ValidCreateRequest()));

        await service.DeleteExpenseAsync(added.Id);

        repository.Expenses.Should().BeEmpty();
        repository.SaveChangesCallCount.Should().Be(2);
    }

    [Fact]
    public async Task DeleteExpenseAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new ExpenseService(new StubCashFlowRepository(seedDefaultBanks: true));

        var act = async () => await service.DeleteExpenseAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetExpensesByMonth_ReturnsOnlyExpensesInThatMonth()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new ExpenseService(repository);
        await service.AddExpenseAsync(ToCreateDto(ValidCreateRequest() with { Date = new DateOnly(2026, 7, 10) }));
        await service.AddExpenseAsync(ToCreateDto(ValidCreateRequest() with { Date = new DateOnly(2026, 8, 10) }));

        var result = service.GetExpensesByMonth(2026, 7);

        result.Should().ContainSingle().Which.Date.Should().Be(new DateOnly(2026, 7, 10));
    }

    [Fact]
    public async Task GetCategoryTotalsByMonth_SumsValuesPerCategoryForThatMonth()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new ExpenseService(repository);
        await service.AddExpenseAsync(ToCreateDto(ValidCreateRequest() with { Category = "Mercado", Value = 10m }));
        await service.AddExpenseAsync(ToCreateDto(ValidCreateRequest() with { Category = "Mercado", Value = 5m }));
        await service.AddExpenseAsync(ToCreateDto(ValidCreateRequest() with { Category = "Casa", Value = 20m }));

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
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new ExpenseService(repository);
        await service.AddExpenseAsync(ToCreateDto(ValidCreateRequest() with { Category = "Reserva", Value = 100m }));
        await service.AddExpenseAsync(ToCreateDto(ValidCreateRequest() with { Category = "Reserva", Value = -30m }));

        var result = service.GetCategoryTotalsByMonth(2026, 7);

        result.Should().ContainSingle(t => t.Category == "Reserva" && t.TotalValue == 70m);
    }

    [Fact]
    public async Task AddExpenseAsync_WithRoundUpAmountOnRoundUpEnabledBank_SavesAmount()
    {
        var service = new ExpenseService(new StubCashFlowRepository(seedDefaultBanks: true));
        var request = ToCreateDto(ValidCreateRequest() with { PaymentSource = "Trading212", Value = 9.40m, RoundUpAmount = 0.60m });

        var result = await service.AddExpenseAsync(request);

        result.RoundUpAmount.Should().Be(0.60m);
    }

    [Fact]
    public async Task AddExpenseAsync_WithRoundUpAmountOfZero_SavesExplicitZero()
    {
        var service = new ExpenseService(new StubCashFlowRepository(seedDefaultBanks: true));
        var request = ToCreateDto(ValidCreateRequest() with { PaymentSource = "Trading212", Value = 10.00m, RoundUpAmount = 0.00m });

        var result = await service.AddExpenseAsync(request);

        result.RoundUpAmount.Should().Be(0.00m);
    }

    [Fact]
    public async Task AddExpenseAsync_WithRoundUpAmountOnNonRoundUpBank_ThrowsNamingTheBank()
    {
        var service = new ExpenseService(new StubCashFlowRepository(seedDefaultBanks: true));
        var request = ToCreateDto(ValidCreateRequest() with { PaymentSource = "Barclays", RoundUpAmount = 0.50m });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Barclays*does not support round-up*");
    }

    [Fact]
    public async Task AddExpenseAsync_WithRoundUpAmountOnCreditCardTaggedExpense_Throws()
    {
        var service = new ExpenseService(new StubCashFlowRepository(seedDefaultBanks: true));
        var request = ToCreateDto(ValidCreateRequest() with { PaymentSource = null, CardTag = "ChaseMaster4023", RoundUpAmount = 0.50m });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*not a credit-card charge*");
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.00)]
    public async Task AddExpenseAsync_WithRoundUpAmountOutsideRange_Throws(decimal roundUpAmount)
    {
        var service = new ExpenseService(new StubCashFlowRepository(seedDefaultBanks: true));
        var request = ToCreateDto(ValidCreateRequest() with { PaymentSource = "Chase", RoundUpAmount = roundUpAmount });

        var act = async () => await service.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*between £0.00 and £0.99*");
    }

    [Fact]
    public async Task AddExpenseAsync_EligibleWithNoRoundUpAmount_ReturnsSuggestedAmount()
    {
        var service = new ExpenseService(new StubCashFlowRepository(seedDefaultBanks: true));
        var request = ToCreateDto(ValidCreateRequest() with { PaymentSource = "Trading212", Value = 9.40m });

        var result = await service.AddExpenseAsync(request);

        result.RoundUpAmount.Should().BeNull();
        result.SuggestedRoundUpAmount.Should().Be(0.60m);
    }

    [Fact]
    public async Task AddExpenseAsync_EligibleWithRoundUpAmountAlreadySaved_ReturnsNoSuggestion()
    {
        var service = new ExpenseService(new StubCashFlowRepository(seedDefaultBanks: true));
        var request = ToCreateDto(ValidCreateRequest() with { PaymentSource = "Trading212", Value = 9.40m, RoundUpAmount = 0.60m });

        var result = await service.AddExpenseAsync(request);

        result.SuggestedRoundUpAmount.Should().BeNull();
    }

    [Fact]
    public async Task AddExpenseAsync_OnNonRoundUpBank_ReturnsNoSuggestion()
    {
        var service = new ExpenseService(new StubCashFlowRepository(seedDefaultBanks: true));
        var request = ToCreateDto(ValidCreateRequest() with { PaymentSource = "Barclays", Value = 9.40m });

        var result = await service.AddExpenseAsync(request);

        result.SuggestedRoundUpAmount.Should().BeNull();
    }

    [Fact]
    public async Task AddExpenseAsync_CreditCardCharge_ReturnsNoSuggestion()
    {
        var service = new ExpenseService(new StubCashFlowRepository(seedDefaultBanks: true));
        var request = ToCreateDto(ValidCreateRequest() with { PaymentSource = null, CardTag = "ChaseMaster4023", Value = 9.40m });

        var result = await service.AddExpenseAsync(request);

        result.SuggestedRoundUpAmount.Should().BeNull();
    }

    [Fact]
    public async Task UpdateExpenseAsync_ChangingValueOnly_LeavesRoundUpAmountUnchanged()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new ExpenseService(repository);
        var added = await service.AddExpenseAsync(ToCreateDto(
            ValidCreateRequest() with { PaymentSource = "Trading212", Value = 9.40m, RoundUpAmount = 0.60m }));

        var updateRequest = ToUpdateDto(
            ValidCreateRequest() with { PaymentSource = "Trading212", Value = 20m, RoundUpAmount = 0.60m });
        var result = await service.UpdateExpenseAsync(added.Id, updateRequest);

        result.Value.Should().Be(20m);
        result.RoundUpAmount.Should().Be(0.60m);
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithNewRoundUpAmount_ChangesIt()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new ExpenseService(repository);
        var added = await service.AddExpenseAsync(ToCreateDto(
            ValidCreateRequest() with { PaymentSource = "Trading212", RoundUpAmount = 0.60m }));

        var updateRequest = ToUpdateDto(ValidCreateRequest() with { PaymentSource = "Trading212", RoundUpAmount = 0.10m });
        var result = await service.UpdateExpenseAsync(added.Id, updateRequest);

        result.RoundUpAmount.Should().Be(0.10m);
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithNullRoundUpAmount_ClearsAPreviouslySavedAmount()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new ExpenseService(repository);
        var added = await service.AddExpenseAsync(ToCreateDto(
            ValidCreateRequest() with { PaymentSource = "Trading212", RoundUpAmount = 0.60m }));

        var updateRequest = ToUpdateDto(ValidCreateRequest() with { PaymentSource = "Trading212", RoundUpAmount = null });
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

    private static ExpenseCreateDTO ToCreateDto(ExpenseCreateRequest r) => new()
    {
        Date = r.Date,
        Description = r.Description,
        Value = r.Value,
        Category = r.Category,
        PaymentSource = r.PaymentSource,
        CardTag = r.CardTag,
        RoundUpAmount = r.RoundUpAmount
    };

    private static ExpenseUpdateDTO ToUpdateDto(ExpenseCreateRequest r) => new()
    {
        Date = r.Date,
        Description = r.Description,
        Value = r.Value,
        Category = r.Category,
        PaymentSource = r.PaymentSource,
        CardTag = r.CardTag,
        RoundUpAmount = r.RoundUpAmount
    };

    private sealed record ExpenseCreateRequest(
        DateOnly Date, string Description, decimal Value, string Category, string? PaymentSource, string? CardTag,
        decimal? RoundUpAmount = null);

}
