using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

/// <summary>Shared test doubles for MonthlyViewModel tests.</summary>
internal sealed class StubExpenseService : IExpenseService
{
    public List<ExpenseDTO> Expenses { get; set; } = [];
    public List<CategoryTotalDTO> CategoryTotals { get; set; } = [];
    public int GetExpensesByMonthCallCount { get; private set; }
    public ExpenseCreateDTO? LastCreateRequest { get; private set; }
    public (Guid Id, ExpenseUpdateDTO Request)? LastUpdateRequest { get; private set; }
    public Guid? LastDeletedId { get; private set; }

    public Task<ExpenseDTO> AddExpenseAsync(ExpenseCreateDTO request)
    {
        LastCreateRequest = request;
        return Task.FromResult(ToDto(Guid.NewGuid(), request.Date, request.Description, request.Value,
            request.Category, request.PaymentSource, request.CardTag, request.RoundUpAmount));
    }

    public Task<ExpenseDTO> UpdateExpenseAsync(Guid id, ExpenseUpdateDTO request)
    {
        LastUpdateRequest = (id, request);
        return Task.FromResult(ToDto(id, request.Date, request.Description, request.Value,
            request.Category, request.PaymentSource, request.CardTag, request.RoundUpAmount));
    }

    public Task DeleteExpenseAsync(Guid id)
    {
        LastDeletedId = id;
        return Task.CompletedTask;
    }

    public IReadOnlyList<ExpenseDTO> GetExpensesByMonth(int year, int month)
    {
        GetExpensesByMonthCallCount++;
        return Expenses;
    }

    public IReadOnlyList<CategoryTotalDTO> GetCategoryTotalsByMonth(int year, int month) => CategoryTotals;

    private static ExpenseDTO ToDto(
        Guid id, DateOnly date, string description, decimal value, string category,
        string? paymentSource, string? cardTag, decimal? roundUpAmount) => new()
    {
        Id = id,
        Date = date,
        Description = description,
        Value = value,
        Category = category,
        PaymentSource = paymentSource,
        CardTag = cardTag,
        PaymentStatus = "ImmediatePayment",
        RoundUpAmount = roundUpAmount,
    };
}

internal sealed class StubIncomeService : IIncomeService
{
    public List<IncomeDTO> Incomes { get; set; } = [];
    public IncomeCreateDTO? LastCreateRequest { get; private set; }
    public (Guid Id, IncomeUpdateDTO Request)? LastUpdateRequest { get; private set; }
    public Guid? LastDeletedId { get; private set; }

    public Task<IncomeDTO> AddIncomeAsync(IncomeCreateDTO request)
    {
        LastCreateRequest = request;
        return Task.FromResult(new IncomeDTO
        {
            Id = Guid.NewGuid(),
            Date = request.Date,
            IncomeSource = request.IncomeSource,
            GrossValue = request.GrossValue,
            NetValue = request.NetValue,
            Bank = request.Bank,
        });
    }

    public Task<IncomeDTO> UpdateIncomeAsync(Guid id, IncomeUpdateDTO request)
    {
        LastUpdateRequest = (id, request);
        return Task.FromResult(new IncomeDTO
        {
            Id = id,
            Date = request.Date,
            IncomeSource = request.IncomeSource,
            GrossValue = request.GrossValue,
            NetValue = request.NetValue,
            Bank = request.Bank,
        });
    }

    public Task DeleteIncomeAsync(Guid id)
    {
        LastDeletedId = id;
        return Task.CompletedTask;
    }

    public IReadOnlyList<IncomeDTO> GetIncomesByMonth(int year, int month) => Incomes;
}

internal sealed class StubBankService : IBankService
{
    public List<BankDTO> Banks { get; set; } = [];

    public IReadOnlyList<BankDTO> GetBanks() => Banks;

    public Task<BankDTO> UpdateOpeningBalanceAsync(string name, BankOpeningBalanceUpdateDTO request) =>
        throw new NotSupportedException();

    public IReadOnlyList<BankBalanceDTO> GetBankBalancesByMonth(int year, int month) => [];

    public decimal GetBankBalanceAsOf(string bankName, DateOnly asOfDate, Guid? excludingAdjustmentId = null) => 0m;
}

internal sealed class StubTitheService : ITitheService
{
    public TitheSummaryDTO Summary { get; set; } = new() { CalculatedTithe = 0m, TitheBalance = 0m };

    public TitheSummaryDTO GetTitheSummary(int year, int month) => Summary;
}
