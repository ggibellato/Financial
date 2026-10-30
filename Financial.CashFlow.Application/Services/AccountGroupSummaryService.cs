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
            var records = AccountTransactionReader.Read(_repository);
            var expenses = records.OfType<Expense>().ToList();
            var members = DefaultAccountGroup.MembersFor(_repository.GetAccounts(), year, month, records)
                .Select(GroupMemberBuilder(year, month, records, expenses))
                .ToList();
            var incomeTotals = BuildIncomeTotals(year, month);

            var result = new AccountGroupSummaryDTO
            {
                Year = year,
                Month = month,
                Members = members,
                Totals = BuildCurrencyTotals(members),
                CategoryTotalsSum = expenses.Where(e => IsInMonth(e.ReportingDate, year, month)).Sum(e => e.Value),
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

    public IReadOnlyList<AccountGroupMemberDTO> GetOtherAccounts(int year, int month)
    {
        using var span = StartSpan("GetOtherAccounts");
        try
        {
            var endOfMonth = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
            var records = AccountTransactionReader.Read(_repository);
            var result = DefaultAccountGroup.OthersFor(_repository.GetAccounts(), year, month, records)
                .Select(account => ToMemberDto(account, endOfMonth, records))
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetOtherAccounts");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public IReadOnlyList<AccountTemplateGroupDTO> GetTemplateGroups(int year, int month)
    {
        using var span = StartSpan("GetTemplateGroups");
        try
        {
            var records = AccountTransactionReader.Read(_repository);
            var toMember = GroupMemberBuilder(year, month, records, records.OfType<Expense>().ToList());

            var result = DefaultAccountGroup.ListedIn(_repository.GetAccounts(), year, month, records)
                .GroupBy(account => account.TemplateOrder)
                .OrderBy(group => group.Key)
                .Select(group => ToTemplateGroupDto(group.Select(toMember).ToList()))
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetTemplateGroups");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private Func<Account, AccountGroupMemberDTO> GroupMemberBuilder(
        int year, int month, IReadOnlyCollection<IAccountTransaction> records, IReadOnlyCollection<Expense> expenses)
    {
        var endOfMonth = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var listedExpenses = expenses.Where(e => IsInMonth(e.ListingDate, year, month)).ToList();
        var statements = _repository.GetCardStatements().ToList();
        return account => ToGroupMemberDto(
            account, endOfMonth, records, listedExpenses, StatementFor(account, year, month, expenses, statements));
    }

    private static AccountTemplateGroupDTO ToTemplateGroupDto(IReadOnlyList<AccountGroupMemberDTO> members) => new()
    {
        TemplateName = members[0].TemplateName,
        Members = members,
        Totals = members
            .GroupBy(member => member.Currency)
            .Select(group => new AccountTemplateGroupTotalDTO
            {
                Currency = group.Key,
                Total = group.Sum(member => member.TotalOwed ?? member.Balance)
            })
            .ToList()
    };

    private static AccountGroupMemberDTO ToGroupMemberDto(
        Account account,
        DateOnly endOfMonth,
        IReadOnlyCollection<IAccountTransaction> records,
        IReadOnlyCollection<Expense> listedExpenses,
        AccountGroupStatementDTO? statement) =>
        ToMemberDto(
            account,
            endOfMonth,
            records,
            account.Has(Capability.SupportsRoundUp)
                ? listedExpenses.Where(e => e.PaymentSourceAccount?.Id == account.Id).Sum(e => e.RoundUpAmount ?? 0m)
                : null,
            statement,
            account.BalanceIsOwed);

    private static AccountGroupMemberDTO ToMemberDto(
        Account account,
        DateOnly endOfMonth,
        IReadOnlyCollection<IAccountTransaction> records,
        decimal? roundUpTotal = null,
        AccountGroupStatementDTO? statement = null,
        bool balanceIsOwed = false) => new()
        {
            AccountId = account.Id,
            Name = account.Name,
            TemplateName = account.TemplateName,
            Currency = account.Currency.ToString(),
            IsActive = account.IsActive,
            Balance = AccountBalanceRule.Balance(account, endOfMonth, records),
            RoundUpTotal = roundUpTotal,
            Statement = statement,
            BalanceIsOwed = balanceIsOwed,
            TotalOwed = account.Has(Capability.HasStatements) ? CardChargeFilter.TotalOwed(account, records) : null
        };

    private static AccountGroupStatementDTO? StatementFor(
        Account account, int year, int month, IReadOnlyCollection<Expense> expenses, IReadOnlyCollection<CardStatement> statements) =>
        account.Has(Capability.HasStatements)
            ? new AccountGroupStatementDTO
            {
                OutstandingTotal = CardChargeFilter.OutstandingTotal(expenses, statements, account, year, month),
                IsPaid = CardChargeFilter.IsStatementPaid(statements, account, year, month)
            }
            : null;

    private static List<CurrencyTotalDTO> BuildCurrencyTotals(IEnumerable<AccountGroupMemberDTO> members) =>
        members
            .GroupBy(member => member.Currency)
            .Select(group => new CurrencyTotalDTO
            {
                Currency = group.Key,
                Balance = group.Where(member => member.Statement is null).Sum(member => member.Balance),
                RoundUpTotal = group.Sum(member => member.RoundUpTotal ?? 0m),
                CardOutstandingTotal = group.Sum(member => member.Statement?.OutstandingTotal ?? 0m)
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

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(AccountGroupSummaryService), operationName, EntityType);
    }
}
