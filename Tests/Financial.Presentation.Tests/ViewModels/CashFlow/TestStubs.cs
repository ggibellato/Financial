using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
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
    public List<BankBalanceDTO> BankBalances { get; set; } = [];

    public IReadOnlyList<BankDTO> GetBanks() => Banks;

    public Task<BankDTO> UpdateOpeningBalanceAsync(string name, BankOpeningBalanceUpdateDTO request) =>
        throw new NotSupportedException();

    public IReadOnlyList<BankBalanceDTO> GetBankBalancesByMonth(int year, int month) => BankBalances;

    public decimal GetBankBalanceAsOf(string bankName, DateOnly asOfDate, Guid? excludingAdjustmentId = null) => 0m;
}

internal sealed class StubTitheService : ITitheService
{
    public TitheSummaryDTO Summary { get; set; } = new() { CalculatedTithe = 0m, TitheBalance = 0m };

    public TitheSummaryDTO GetTitheSummary(int year, int month) => Summary;
}

internal sealed class StubTransferService : ITransferService
{
    public List<TransferDTO> Transfers { get; set; } = [];
    public TransferCreateDTO? LastCreateRequest { get; private set; }
    public (Guid Id, TransferUpdateDTO Request)? LastUpdateRequest { get; private set; }
    public Guid? LastDeletedId { get; private set; }
    public string? ThrowOnAdd { get; set; }

    public Task<TransferDTO> AddTransferAsync(TransferCreateDTO request)
    {
        if (ThrowOnAdd is { } message)
        {
            throw new InvalidOperationException(message);
        }

        LastCreateRequest = request;
        return Task.FromResult(new TransferDTO
        {
            Id = Guid.NewGuid(), Date = request.Date, SourceBank = request.SourceBank,
            DestinationBank = request.DestinationBank, Amount = request.Amount, Note = request.Note,
        });
    }

    public Task<TransferDTO> UpdateTransferAsync(Guid id, TransferUpdateDTO request)
    {
        LastUpdateRequest = (id, request);
        return Task.FromResult(new TransferDTO
        {
            Id = id, Date = request.Date, SourceBank = request.SourceBank,
            DestinationBank = request.DestinationBank, Amount = request.Amount, Note = request.Note,
        });
    }

    public Task DeleteTransferAsync(Guid id)
    {
        LastDeletedId = id;
        return Task.CompletedTask;
    }

    public IReadOnlyList<TransferDTO> GetTransfersByMonth(int year, int month) => Transfers;

    public IReadOnlyList<TransferDTO> GetTransfersByBank(string bankName) =>
        Transfers.Where(t => t.SourceBank == bankName || t.DestinationBank == bankName).ToList();
}

internal sealed class StubBalanceAdjustmentService : IBalanceAdjustmentService
{
    public Dictionary<string, List<BalanceAdjustmentDTO>> AdjustmentsByBank { get; set; } = [];
    public (string Bank, BalanceAdjustmentCreateDTO Request)? LastCreateRequest { get; private set; }
    public (string Bank, Guid Id, BalanceAdjustmentUpdateDTO Request)? LastUpdateRequest { get; private set; }
    public (string Bank, Guid Id)? LastDeleted { get; private set; }

    public Task<BalanceAdjustmentDTO> AddAdjustmentAsync(string bankName, BalanceAdjustmentCreateDTO request)
    {
        LastCreateRequest = (bankName, request);
        return Task.FromResult(new BalanceAdjustmentDTO
        {
            Id = Guid.NewGuid(), Date = request.Date, Bank = bankName,
            TargetBalance = request.TargetBalance, Delta = 0m, Note = request.Note,
        });
    }

    public Task<BalanceAdjustmentDTO> UpdateAdjustmentAsync(string bankName, Guid id, BalanceAdjustmentUpdateDTO request)
    {
        LastUpdateRequest = (bankName, id, request);
        return Task.FromResult(new BalanceAdjustmentDTO
        {
            Id = id, Date = request.Date, Bank = bankName,
            TargetBalance = request.TargetBalance, Delta = 0m, Note = request.Note,
        });
    }

    public Task DeleteAdjustmentAsync(string bankName, Guid id)
    {
        LastDeleted = (bankName, id);
        return Task.CompletedTask;
    }

    public IReadOnlyList<BalanceAdjustmentDTO> GetAdjustmentsByBank(string bankName) =>
        AdjustmentsByBank.GetValueOrDefault(bankName, []);
}

internal sealed class StubCardStatementService : ICardStatementService
{
    public List<CardStatementDTO> Statements { get; set; } = [];
    public (Guid Id, MarkStatementPaidDTO Request)? LastMarkPaidRequest { get; private set; }
    public Guid? LastUnmarkedId { get; private set; }

    public Task<IReadOnlyList<CardStatementDTO>> GetStatementsForMonthAsync(int year, int month) =>
        Task.FromResult<IReadOnlyList<CardStatementDTO>>(Statements);

    public Task<CardStatementDTO> MarkStatementPaidAsync(Guid id, MarkStatementPaidDTO request)
    {
        LastMarkPaidRequest = (id, request);
        var existing = Statements.First(s => s.Id == id);
        return Task.FromResult(new CardStatementDTO
        {
            Id = id, Card = existing.Card, Year = existing.Year, Month = existing.Month,
            IsPaid = true, OutstandingTotal = existing.OutstandingTotal,
        });
    }

    public Task<CardStatementDTO> UnmarkStatementPaidAsync(Guid id)
    {
        LastUnmarkedId = id;
        var existing = Statements.First(s => s.Id == id);
        return Task.FromResult(new CardStatementDTO
        {
            Id = id, Card = existing.Card, Year = existing.Year, Month = existing.Month,
            IsPaid = false, OutstandingTotal = existing.OutstandingTotal,
        });
    }
}

internal sealed class StubReserveService : IReserveService
{
    public List<ReserveBucketBalanceDTO> Balances { get; set; } = [];
    public List<ReserveMovementDTO> Movements { get; set; } = [];
    public IncomeSplitResultDTO SplitResult { get; set; } =
        new() { Investimento = 10m, HouseTreats = 20m, Ariana = 5m, Gleison = 5m, Total = 40m };
    public IncomeSplitRequestDTO? LastSplitRequest { get; private set; }
    public List<WithdrawalRequestDTO> WithdrawalRequests { get; } = [];
    public bool ThrowOverdraftOnUnconfirmedWithdrawal { get; set; }
    public string OverdraftMessage { get; set; } = "This withdrawal exceeds the bucket's balance.";
    public Exception? ThrowOnWithdrawal { get; set; }
    public (Guid Id, UpdateReserveMovementDTO Request)? LastUpdateRequest { get; private set; }
    public Guid? LastDeletedId { get; private set; }

    public Task<IncomeSplitResultDTO> PostIncomeSplitAsync(IncomeSplitRequestDTO request)
    {
        LastSplitRequest = request;
        return Task.FromResult(SplitResult);
    }

    public Task<ReserveMovementDTO> PostWithdrawalAsync(WithdrawalRequestDTO request)
    {
        WithdrawalRequests.Add(request);

        if (ThrowOnWithdrawal is { } ex)
        {
            throw ex;
        }

        if (ThrowOverdraftOnUnconfirmedWithdrawal && !request.Confirmed)
        {
            throw new OverdraftConfirmationRequiredException(OverdraftMessage);
        }

        return Task.FromResult(new ReserveMovementDTO
        {
            Id = Guid.NewGuid(), Bucket = request.Bucket, Amount = -request.Amount,
            Date = request.Date, Description = request.Description,
        });
    }

    public IReadOnlyList<ReserveBucketBalanceDTO> GetBucketBalances() => Balances;

    public IReadOnlyList<ReserveMovementDTO> GetMovementHistory() => Movements;

    public Task<ReserveMovementDTO> UpdateMovementAsync(Guid id, UpdateReserveMovementDTO request)
    {
        LastUpdateRequest = (id, request);
        return Task.FromResult(new ReserveMovementDTO
        {
            Id = id, Bucket = request.Bucket, Amount = request.Amount,
            Date = request.Date, Description = request.Description,
        });
    }

    public Task DeleteMovementAsync(Guid id)
    {
        LastDeletedId = id;
        return Task.CompletedTask;
    }
}

internal sealed class StubMensaisService : IMensaisService
{
    public List<RecurringBillDTO> Bills { get; set; } = [];
    public CreateRecurringBillDTO? LastCreateRequest { get; private set; }
    public (Guid Id, UpdateRecurringBillDTO Request)? LastUpdateRequest { get; private set; }
    public Guid? LastDeletedId { get; private set; }
    public int ResetAllToUnsetCallCount { get; private set; }

    public Task<RecurringBillDTO> CreateBillAsync(CreateRecurringBillDTO request)
    {
        LastCreateRequest = request;
        var bill = new RecurringBillDTO
        {
            Id = Guid.NewGuid(), DueDay = request.DueDay, Description = request.Description,
            Value = request.Value, Area = request.Area, Note = request.Note,
            NitNumber = null, MinimumWageValue = null, Status = "Unset",
        };
        Bills.Add(bill);
        return Task.FromResult(bill);
    }

    public Task DeleteBillAsync(Guid id)
    {
        LastDeletedId = id;
        Bills.RemoveAll(b => b.Id == id);
        return Task.CompletedTask;
    }

    public IReadOnlyList<RecurringBillDTO> GetBills() => Bills;

    public Task<RecurringBillDTO> UpdateBillAsync(Guid id, UpdateRecurringBillDTO request)
    {
        LastUpdateRequest = (id, request);
        var existing = Bills.First(b => b.Id == id);
        var updated = new RecurringBillDTO
        {
            Id = id, DueDay = existing.DueDay, Description = existing.Description,
            Value = request.Value, Area = existing.Area, Note = existing.Note,
            NitNumber = existing.NitNumber, MinimumWageValue = existing.MinimumWageValue, Status = request.Status,
        };
        Bills[Bills.IndexOf(existing)] = updated;
        return Task.FromResult(updated);
    }

    public Task<IReadOnlyList<RecurringBillDTO>> ResetAllToUnsetAsync()
    {
        ResetAllToUnsetCallCount++;
        Bills = Bills.Select(b => new RecurringBillDTO
        {
            Id = b.Id, DueDay = b.DueDay, Description = b.Description, Value = b.Value,
            Area = b.Area, Note = b.Note, NitNumber = b.NitNumber, MinimumWageValue = b.MinimumWageValue,
            Status = "Unset",
        }).ToList();
        return Task.FromResult<IReadOnlyList<RecurringBillDTO>>(Bills);
    }
}
