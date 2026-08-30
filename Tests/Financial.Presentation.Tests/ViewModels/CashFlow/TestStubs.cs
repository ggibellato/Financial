using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

/// <summary>Shared test doubles for MonthlyViewModel tests.</summary>
internal sealed class StubExpenseService : IExpenseService
{
    public List<ExpenseDTO> Expenses { get; set; } = [];
    public List<ExpenseDTO> UnpaidCardCharges { get; set; } = [];
    public List<CategoryTotalDTO> CategoryTotals { get; set; } = [];
    public int GetExpensesByMonthCallCount { get; private set; }
    public int GetUnpaidCardChargesByMonthCallCount { get; private set; }
    public ExpenseCreateDTO? LastCreateRequest { get; private set; }
    public (Guid Id, ExpenseUpdateDTO Request)? LastUpdateRequest { get; private set; }
    public Guid? LastDeletedId { get; private set; }

    public Task<ExpenseDTO> AddExpenseAsync(ExpenseCreateDTO request)
    {
        LastCreateRequest = request;
        return Task.FromResult(ToDto(Guid.NewGuid(), request.Date, request.Description, request.Value,
            request.CategoryId, request.PaymentSourceBankId, request.CreditCardId, request.RoundUpAmount));
    }

    public Task<ExpenseDTO> UpdateExpenseAsync(Guid id, ExpenseUpdateDTO request)
    {
        LastUpdateRequest = (id, request);
        return Task.FromResult(ToDto(id, request.Date, request.Description, request.Value,
            request.CategoryId, request.PaymentSourceBankId, request.CreditCardId, request.RoundUpAmount));
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

    public IReadOnlyList<ExpenseDTO> GetUnpaidCardChargesByMonth(int year, int month)
    {
        GetUnpaidCardChargesByMonthCallCount++;
        return UnpaidCardCharges;
    }

    public IReadOnlyList<CategoryTotalDTO> GetCategoryTotalsByMonth(int year, int month) => CategoryTotals;

    private static ExpenseDTO ToDto(
        Guid id, DateOnly date, string description, decimal value, Guid categoryId,
        Guid? paymentSourceBankId, Guid? creditCardId, decimal? roundUpAmount) => new()
    {
        Id = id,
        Date = date,
        Description = description,
        Value = value,
        CategoryId = categoryId,
        CategoryName = categoryId.ToString(),
        PaymentSourceBankId = paymentSourceBankId,
        CreditCardId = creditCardId,
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
            IncomeSourceId = request.IncomeSourceId,
            IncomeSourceName = request.IncomeSourceId.ToString(),
            GrossValue = request.GrossValue,
            NetValue = request.NetValue,
            BankId = request.BankId,
            BankName = request.BankId.ToString(),
            SplitToReserve = request.SplitToReserve,
        });
    }

    public Task<IncomeDTO> UpdateIncomeAsync(Guid id, IncomeUpdateDTO request)
    {
        LastUpdateRequest = (id, request);
        return Task.FromResult(new IncomeDTO
        {
            Id = id,
            Date = request.Date,
            IncomeSourceId = request.IncomeSourceId,
            IncomeSourceName = request.IncomeSourceId.ToString(),
            GrossValue = request.GrossValue,
            NetValue = request.NetValue,
            BankId = request.BankId,
            BankName = request.BankId.ToString(),
            SplitToReserve = request.SplitToReserve,
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

    public Task<BankDTO> CreateBankAsync(BankCreateDTO request) => throw new NotSupportedException();

    public Task<BankDTO> UpdateBankAsync(Guid id, BankUpdateDTO request) => throw new NotSupportedException();

    public Task DeleteBankAsync(Guid id) => throw new NotSupportedException();

    public Task<BankDTO> UpdateOpeningBalanceAsync(Guid id, BankOpeningBalanceUpdateDTO request) =>
        throw new NotSupportedException();

    public IReadOnlyList<BankBalanceDTO> GetBankBalancesByMonth(int year, int month) => BankBalances;

    public decimal GetBankBalanceAsOf(Guid bankId, DateOnly asOfDate, Guid? excludingAdjustmentId = null) => 0m;
}

internal sealed class StubIncomeSourceService : IIncomeSourceService
{
    public List<IncomeSourceDTO> IncomeSources { get; set; } = [];
    public Exception? ThrowOnGet { get; set; }

    public IReadOnlyList<IncomeSourceDTO> GetIncomeSources()
    {
        if (ThrowOnGet is { } ex)
        {
            throw ex;
        }

        return IncomeSources;
    }
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
    public int GetTransfersByMonthCallCount { get; private set; }

    public Task<TransferDTO> AddTransferAsync(TransferCreateDTO request)
    {
        if (ThrowOnAdd is { } message)
        {
            throw new InvalidOperationException(message);
        }

        LastCreateRequest = request;
        return Task.FromResult(new TransferDTO
        {
            Id = Guid.NewGuid(), Date = request.Date, SourceBankId = request.SourceBankId, SourceBankName = request.SourceBankId.ToString(),
            DestinationBankId = request.DestinationBankId, DestinationBankName = request.DestinationBankId.ToString(), Amount = request.Amount, Note = request.Note,
        });
    }

    public Task<TransferDTO> UpdateTransferAsync(Guid id, TransferUpdateDTO request)
    {
        LastUpdateRequest = (id, request);
        return Task.FromResult(new TransferDTO
        {
            Id = id, Date = request.Date, SourceBankId = request.SourceBankId, SourceBankName = request.SourceBankId.ToString(),
            DestinationBankId = request.DestinationBankId, DestinationBankName = request.DestinationBankId.ToString(), Amount = request.Amount, Note = request.Note,
        });
    }

    public Task DeleteTransferAsync(Guid id)
    {
        LastDeletedId = id;
        return Task.CompletedTask;
    }

    public IReadOnlyList<TransferDTO> GetTransfersByMonth(int year, int month)
    {
        GetTransfersByMonthCallCount++;
        return Transfers;
    }

    public IReadOnlyList<TransferDTO> GetTransfersByBank(Guid bankId) =>
        Transfers.Where(t => t.SourceBankId == bankId || t.DestinationBankId == bankId).ToList();
}

internal sealed class StubBalanceAdjustmentService : IBalanceAdjustmentService
{
    public Dictionary<Guid, List<BalanceAdjustmentDTO>> AdjustmentsByBank { get; set; } = [];
    public (Guid BankId, BalanceAdjustmentCreateDTO Request)? LastCreateRequest { get; private set; }
    public (Guid BankId, Guid Id, BalanceAdjustmentUpdateDTO Request)? LastUpdateRequest { get; private set; }
    public (Guid BankId, Guid Id)? LastDeleted { get; private set; }
    public int GetAdjustmentsByBankCallCount { get; private set; }

    public Task<BalanceAdjustmentDTO> AddAdjustmentAsync(Guid bankId, BalanceAdjustmentCreateDTO request)
    {
        LastCreateRequest = (bankId, request);
        return Task.FromResult(new BalanceAdjustmentDTO
        {
            Id = Guid.NewGuid(), Date = request.Date, BankId = bankId, BankName = bankId.ToString(),
            TargetBalance = request.TargetBalance, Delta = 0m, Note = request.Note,
        });
    }

    public Task<BalanceAdjustmentDTO> UpdateAdjustmentAsync(Guid bankId, Guid id, BalanceAdjustmentUpdateDTO request)
    {
        LastUpdateRequest = (bankId, id, request);
        return Task.FromResult(new BalanceAdjustmentDTO
        {
            Id = id, Date = request.Date, BankId = bankId, BankName = bankId.ToString(),
            TargetBalance = request.TargetBalance, Delta = 0m, Note = request.Note,
        });
    }

    public Task DeleteAdjustmentAsync(Guid bankId, Guid id)
    {
        LastDeleted = (bankId, id);
        return Task.CompletedTask;
    }

    public IReadOnlyList<BalanceAdjustmentDTO> GetAdjustmentsByBank(Guid bankId)
    {
        GetAdjustmentsByBankCallCount++;
        return AdjustmentsByBank.GetValueOrDefault(bankId, []);
    }
}

internal sealed class StubCardStatementService : ICardStatementService
{
    public List<CardStatementDTO> Statements { get; set; } = [];
    public (Guid Id, MarkCardStatementPaidDTO Request)? LastMarkPaidRequest { get; private set; }
    public Guid? LastUnmarkedId { get; private set; }

    /// <summary>Server-supplied warning to hand back from the next mark/unmark call.</summary>
    public string? NextWarning { get; set; }

    public Task<IReadOnlyList<CardStatementDTO>> GetStatementsForMonthAsync(int year, int month) =>
        Task.FromResult<IReadOnlyList<CardStatementDTO>>(Statements);

    public Task<CardStatementDTO> MarkStatementPaidAsync(Guid id, MarkCardStatementPaidDTO request)
    {
        LastMarkPaidRequest = (id, request);
        var existing = Statements.First(s => s.Id == id);
        return Task.FromResult(new CardStatementDTO
        {
            Id = id, CreditCardId = existing.CreditCardId, CreditCardName = existing.CreditCardName,
            Year = existing.Year, Month = existing.Month,
            IsPaid = true, OutstandingTotal = existing.OutstandingTotal, Warning = NextWarning,
        });
    }

    public Task<CardStatementDTO> UnmarkStatementPaidAsync(Guid id)
    {
        LastUnmarkedId = id;
        var existing = Statements.First(s => s.Id == id);
        return Task.FromResult(new CardStatementDTO
        {
            Id = id, CreditCardId = existing.CreditCardId, CreditCardName = existing.CreditCardName,
            Year = existing.Year, Month = existing.Month,
            IsPaid = false, OutstandingTotal = existing.OutstandingTotal, Warning = NextWarning,
        });
    }
}

internal sealed class StubCreditCardService : ICreditCardService
{
    public List<CreditCardDTO> CreditCards { get; set; } = [];
    public (Guid Id, CreditCardUpdateDTO Request)? LastUpdateRequest { get; private set; }
    public string? ThrowOnUpdate { get; set; }

    public IReadOnlyList<CreditCardDTO> GetCreditCards() => CreditCards;

    public Task<CreditCardDTO> UpdateCreditCardAsync(Guid id, CreditCardUpdateDTO request)
    {
        if (ThrowOnUpdate is { } message)
        {
            throw new InvalidOperationException(message);
        }

        LastUpdateRequest = (id, request);
        var existing = CreditCards.First(c => c.Id == id);
        var updated = new CreditCardDTO
        {
            Id = existing.Id,
            Name = existing.Name,
            IsActive = request.IsActive,
            NextInvoiceDueDate = request.NextInvoiceDueDate,
        };
        var index = CreditCards.FindIndex(c => c.Id == id);
        CreditCards[index] = updated;
        return Task.FromResult(updated);
    }
}

internal sealed class StubCategoryService : ICategoryService
{
    public List<CategoryDTO> Categories { get; set; } = [];

    public IReadOnlyList<CategoryDTO> GetCategories() => Categories;
}

internal sealed class StubReserveService : IReserveService
{
    public List<ReserveBucketBalanceDTO> Balances { get; set; } = [];
    public List<ReserveMovementDTO> Movements { get; set; } = [];
    public IncomeSplitResultDTO SplitResult { get; set; } =
        new()
        {
            Buckets =
            [
                new() { BucketId = Guid.NewGuid(), BucketName = "Investimento", Amount = 10m },
                new() { BucketId = Guid.NewGuid(), BucketName = "HouseTreats", Amount = 20m },
                new() { BucketId = Guid.NewGuid(), BucketName = "Ariana", Amount = 5m },
                new() { BucketId = Guid.NewGuid(), BucketName = "Gleison", Amount = 5m }
            ],
            Total = 40m
        };
    public IncomeSplitRequestDTO? LastSplitRequest { get; private set; }
    public List<WithdrawalRequestDTO> WithdrawalRequests { get; } = [];

    /// <summary>Buckets this stub can name when echoing a request back as a movement DTO.
    /// Left empty unless a test asserts on the echoed name.</summary>
    public List<ReserveBucketDTO> KnownBuckets { get; set; } = [];
    public bool ThrowOverdraftOnUnconfirmedWithdrawal { get; set; }
    public string OverdraftMessage { get; set; } = "This withdrawal exceeds the bucket's balance.";
    public Exception? ThrowOnWithdrawal { get; set; }
    public (Guid Id, ReserveMovementUpdateDTO Request)? LastUpdateRequest { get; private set; }
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
            Id = Guid.NewGuid(), BucketId = request.BucketId, BucketName = NameOf(request.BucketId), Amount = -request.Amount,
            Date = request.Date, Description = request.Description,
        });
    }

    public IReadOnlyList<ReserveBucketBalanceDTO> GetBucketBalances() => Balances;

    public IReadOnlyList<ReserveMovementDTO> GetMovementHistory() => Movements;

    public Task<ReserveMovementDTO> UpdateMovementAsync(Guid id, ReserveMovementUpdateDTO request)
    {
        LastUpdateRequest = (id, request);
        return Task.FromResult(new ReserveMovementDTO
        {
            Id = id, BucketId = request.BucketId, BucketName = NameOf(request.BucketId), Amount = request.Amount,
            Date = request.Date, Description = request.Description,
        });
    }

    private string NameOf(Guid id) => KnownBuckets.FirstOrDefault(b => b.Id == id)?.Name ?? string.Empty;

    public Task DeleteMovementAsync(Guid id)
    {
        LastDeletedId = id;
        return Task.CompletedTask;
    }
}

internal sealed class StubReserveBucketService : IReserveBucketService
{
    public List<ReserveBucketDTO> ReserveBuckets { get; set; } = [];
    public Exception? ThrowOnGet { get; set; }
    public int GetReserveBucketsCallCount { get; private set; }

    public IReadOnlyList<ReserveBucketDTO> GetReserveBuckets()
    {
        GetReserveBucketsCallCount++;
        if (ThrowOnGet is { } ex)
        {
            throw ex;
        }

        return ReserveBuckets;
    }
}

internal sealed class StubMensaisService : IMensaisService
{
    public List<RecurringBillDTO> Bills { get; set; } = [];
    public RecurringBillCreateDTO? LastCreateRequest { get; private set; }
    public (Guid Id, RecurringBillUpdateDTO Request)? LastUpdateRequest { get; private set; }
    public Guid? LastDeletedId { get; private set; }
    public int ResetAllToUnsetCallCount { get; private set; }

    public Task<RecurringBillDTO> CreateBillAsync(RecurringBillCreateDTO request)
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

    public Exception? ThrowOnGetBills { get; set; }

    public IReadOnlyList<RecurringBillDTO> GetBills()
    {
        if (ThrowOnGetBills is { } ex)
        {
            throw ex;
        }

        return Bills;
    }

    public Task<RecurringBillDTO> UpdateBillAsync(Guid id, RecurringBillUpdateDTO request)
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

internal sealed class StubControleMaeService : IControleMaeService
{
    public List<MaeLedgerEntryDTO> Entries { get; set; } = [];
    public MaeLedgerTotalsDTO Totals { get; set; } = new() { TotalBrlValue = 0m, TotalGbpValue = 0m };
    public MaeLedgerEntryCreateDTO? LastCreateRequest { get; private set; }
    public (Guid Id, MaeLedgerEntryValuesUpdateDTO Request)? LastUpdateRequest { get; private set; }
    public Guid? LastDeletedId { get; private set; }
    public int GetEntriesFromDateCallCount { get; private set; }
    public int GetTotalsCallCount { get; private set; }
    public DateOnly? LastFromDate { get; private set; }

    public Task<MaeLedgerEntryDTO> CreateEntryAsync(MaeLedgerEntryCreateDTO request)
    {
        LastCreateRequest = request;
        var entry = new MaeLedgerEntryDTO
        {
            Id = Guid.NewGuid(), Date = request.Date, Description = request.Description, Note = request.Note,
            SourceCurrency = request.SourceCurrency,
            BrlValue = request.SourceCurrency == "BRL" ? request.SourceValue : request.SourceValue * 5m,
            GbpValue = request.SourceCurrency == "GBP" ? request.SourceValue : request.SourceValue / 5m,
        };
        Entries.Add(entry);
        return Task.FromResult(entry);
    }

    public IReadOnlyList<MaeLedgerEntryDTO> GetEntriesFromDate(DateOnly fromDate)
    {
        GetEntriesFromDateCallCount++;
        LastFromDate = fromDate;
        return Entries.Where(e => e.Date >= fromDate).ToList();
    }

    public Exception? ThrowOnGetTotals { get; set; }

    public MaeLedgerTotalsDTO GetTotals()
    {
        GetTotalsCallCount++;
        if (ThrowOnGetTotals is { } ex)
        {
            throw ex;
        }

        return Totals;
    }

    public Task<MaeLedgerEntryDTO> UpdateEntryValuesAsync(Guid id, MaeLedgerEntryValuesUpdateDTO request)
    {
        LastUpdateRequest = (id, request);
        var existing = Entries.First(e => e.Id == id);
        var updated = new MaeLedgerEntryDTO
        {
            Id = id, Date = existing.Date, Description = existing.Description, Note = existing.Note,
            SourceCurrency = existing.SourceCurrency, BrlValue = request.BrlValue, GbpValue = request.GbpValue,
        };
        Entries[Entries.IndexOf(existing)] = updated;
        return Task.FromResult(updated);
    }

    public Task DeleteEntryAsync(Guid id)
    {
        LastDeletedId = id;
        Entries.RemoveAll(e => e.Id == id);
        return Task.CompletedTask;
    }
}

internal sealed class StubInvestmentSnapshotService : IInvestmentSnapshotService
{
    public List<InvestmentSnapshotDTO> Snapshots { get; set; } = [];
    public int GetSnapshotsForMonthCallCount { get; private set; }
    public (Guid Id, InvestmentSnapshotValueUpdateDTO Request)? LastUpdateRequest { get; private set; }
    public Exception? ThrowOnUpdate { get; set; }

    public Task<IReadOnlyList<InvestmentSnapshotDTO>> GetSnapshotsForMonthAsync(int year, int month)
    {
        GetSnapshotsForMonthCallCount++;
        return Task.FromResult<IReadOnlyList<InvestmentSnapshotDTO>>(
            Snapshots.Where(s => s.Year == year && s.Month == month).ToList());
    }

    public Task<InvestmentSnapshotDTO> UpdateSnapshotValueAsync(Guid id, InvestmentSnapshotValueUpdateDTO request)
    {
        if (ThrowOnUpdate is { } ex)
        {
            throw ex;
        }

        LastUpdateRequest = (id, request);
        var existing = Snapshots.First(s => s.Id == id);
        var updated = new InvestmentSnapshotDTO
        {
            Id = id, AccountId = existing.AccountId, AccountName = existing.AccountName, IsLiability = existing.IsLiability,
            Year = existing.Year, Month = existing.Month, Value = request.Value,
        };
        Snapshots[Snapshots.IndexOf(existing)] = updated;
        return Task.FromResult(updated);
    }
}

internal sealed class StubCategorySummaryService : ICategorySummaryService
{
    public CategoryTotalsAnnualDTO CategoryTotalsAnnual { get; set; } = new()
    {
        CategoryTotals = [],
        IncomeSummary = new IncomeAnnualSummaryDTO
        {
            SalaryMonthly = new decimal[12], SalaryAnnualTotal = 0m, SalaryAverage = 0m,
            SalaryAfterTaxesMonthly = new decimal[12], SalaryAfterTaxesAnnualTotal = 0m, SalaryAfterTaxesAverage = 0m,
            TaxDifferenceMonthly = new decimal[12], TaxDifferenceAnnualTotal = 0m, TaxDifferenceAverage = 0m,
            DividendoJurosMonthly = new decimal[12], DividendoJurosAnnualTotal = 0m, DividendoJurosAverage = 0m,
        },
        TotalDespesasMonthly = new decimal[12], TotalDespesasAnnualTotal = 0m, TotalDespesasAverage = 0m,
        ResultadoMonthly = new decimal[12], ResultadoAnnualTotal = 0m, ResultadoAverage = 0m,
    };

    public int GetCategoryTotalsAnnualForYearCallCount { get; private set; }

    public IReadOnlyList<CategoryAnnualTotalDTO> GetCategoryTotalsForYear(int year) => throw new NotImplementedException();

    public CategoryTotalsAnnualDTO GetCategoryTotalsAnnualForYear(int year)
    {
        GetCategoryTotalsAnnualForYearCallCount++;
        return CategoryTotalsAnnual;
    }
}

internal sealed class StubInvestmentAnnualResultService : IInvestmentAnnualResultService
{
    public InvestmentAnnualResultDTO InvestmentAnnualResult { get; set; } = new()
    {
        Accounts = [],
        NetPosition = new NetPositionAnnualDiffDTO
        {
            MonthlyValues = new decimal[12], MonthlyDiffs = new decimal?[12],
            FullYearNetChange = 0m, AverageMonthResult = 0m, SumOfMonthResults = 0m,
        },
    };

    public int GetInvestmentAnnualResultForYearCallCount { get; private set; }

    public InvestmentAnnualResultDTO GetInvestmentAnnualResultForYear(int year)
    {
        GetInvestmentAnnualResultForYearCallCount++;
        return InvestmentAnnualResult;
    }
}

internal sealed class StubHistoricAverageService : IHistoricAverageService
{
    public List<CategoryAnnualAverageDTO> HistoricSummaryAverage { get; set; } = [];

    public int GetHistoricSummaryAverageFromYearCallCount { get; private set; }

    public IReadOnlyList<CategoryAnnualAverageDTO> GetHistoricSummaryAverageFromYear(int year)
    {
        GetHistoricSummaryAverageFromYearCallCount++;
        return HistoricSummaryAverage;
    }
}
