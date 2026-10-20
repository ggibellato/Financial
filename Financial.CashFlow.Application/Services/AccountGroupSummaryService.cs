using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class AccountGroupSummaryService : IAccountGroupSummaryService
{
    private const string EntityType = "AccountGroup";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<AccountGroupSummaryService> _logger;

    public AccountGroupSummaryService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<AccountGroupSummaryService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public AccountGroupSummaryDTO GetDefaultGroupSummary(int year, int month)
    {
        using var span = StartSpan("GetDefaultGroupSummary");
        try
        {
            var endOfMonth = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
            var records = ReadAccountTransactions();
            var listedExpenses = _repository.GetExpenses().Where(e => IsInMonth(e.ListingDate, year, month)).ToList();

            var members = DefaultAccountGroup.MembersFor(_repository.GetAccounts(), year, month, records)
                .Select(account => ToMemberDto(account, endOfMonth, records, listedExpenses))
                .ToList();
            var incomeTotals = BuildIncomeTotals(year, month);

            var result = new AccountGroupSummaryDTO
            {
                Year = year,
                Month = month,
                Members = members,
                Totals = BuildCurrencyTotals(members),
                CategoryTotalsSum = _repository.GetExpenses().Where(e => IsInMonth(e.ReportingDate, year, month)).Sum(e => e.Value),
                IncomeTotals = incomeTotals,
                TotalIncoming = incomeTotals.Sum(income => income.NetValue)
            };

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetDefaultGroupSummary");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private static AccountGroupMemberDTO ToMemberDto(
        Account account, DateOnly endOfMonth, IReadOnlyCollection<IAccountTransaction> records, IReadOnlyCollection<Expense> listedExpenses) => new()
        {
            AccountId = account.Id,
            Name = account.Name,
            TemplateName = account.TemplateName,
            Currency = account.Currency.ToString(),
            Balance = AccountBalanceRule.Balance(account, endOfMonth, records),
            RoundUpTotal = account.Has(Capability.SupportsRoundUp)
                ? listedExpenses.Where(e => e.PaymentSourceAccount?.Id == account.Id).Sum(e => e.RoundUpAmount ?? 0m)
                : null
        };

    private static List<CurrencyTotalDTO> BuildCurrencyTotals(IEnumerable<AccountGroupMemberDTO> members) =>
        members
            .GroupBy(member => member.Currency)
            .Select(group => new CurrencyTotalDTO
            {
                Currency = group.Key,
                Balance = group.Sum(member => member.Balance),
                RoundUpTotal = group.Sum(member => member.RoundUpTotal ?? 0m)
            })
            .ToList();

    private List<IncomeSourceTotalDTO> BuildIncomeTotals(int year, int month) =>
        _repository.GetIncomes()
            .Where(income => IsInMonth(income.Date, year, month))
            .GroupBy(income => income.IncomeSource.Id)
            .Select(group => new IncomeSourceTotalDTO
            {
                IncomeSourceId = group.Key,
                Name = group.First().IncomeSource.Name,
                NetValue = group.Sum(income => income.NetValue),
                GrossValue = group.Any(income => income.GrossValue is not null) ? group.Sum(income => income.GrossValue ?? 0m) : null
            })
            .ToList();

    private static bool IsInMonth(DateOnly date, int year, int month) => date.Year == year && date.Month == month;

    private List<IAccountTransaction> ReadAccountTransactions() =>
    [
        .. _repository.GetIncomes(),
        .. _repository.GetExpenses(),
        .. _repository.GetTransfers(),
        .. _repository.GetBalanceAdjustments()
    ];

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(AccountGroupSummaryService), operationName, EntityType);
    }
}
