using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class PaymentsDueService : IPaymentsDueService
{
    private const string EntityType = "PaymentDue";
    private const string MensaisType = "Mensais";
    private const string CreditCardType = "CreditCard";
    private const int NotificationWindowDays = 5;

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<PaymentsDueService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _timeZone;

    public PaymentsDueService(
        ICashFlowRepository repository,
        ITelemetryTracer tracer,
        ILogger<PaymentsDueService> logger,
        TimeProvider? timeProvider = null,
        TimeZoneInfo? timeZone = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _timeZone = timeZone ?? TimeZoneInfo.Local;
    }

    public IReadOnlyList<PaymentDueDTO> GetPaymentsDue()
    {
        using var span = StartSpan("GetPaymentsDue");
        try
        {
            var today = GetToday();

            var payments = GetMensaisPaymentsDue(today)
                .Concat(GetCreditCardPaymentsDue(today))
                .OrderBy(p => p.DueDate)
                .ThenBy(p => TypeSortOrder(p.Type))
                .ThenBy(p => p.Name, StringComparer.Ordinal)
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetPaymentsDue");
            return payments;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            _logger.LogError(ex, "Failed to aggregate payments due; returning an empty list.");
            return Array.Empty<PaymentDueDTO>();
        }
    }

    private DateOnly GetToday()
    {
        var localNow = TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), _timeZone);
        return DateOnly.FromDateTime(localNow.DateTime);
    }

    private List<PaymentDueDTO> GetMensaisPaymentsDue(DateOnly today)
    {
        try
        {
            return _repository.GetRecurringBills()
                .Where(bill => bill.Status == BillStatus.Unset)
                .Select(bill => TryBuildMensaisPayment(bill, today))
                .Where(payment => payment is not null)
                .Select(payment => payment!)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recurring bills for the payments-due aggregation.");
            return [];
        }
    }

    private List<PaymentDueDTO> GetCreditCardPaymentsDue(DateOnly today)
    {
        try
        {
            return _repository.GetCreditCards()
                .Where(card => card.NextInvoiceDueDate is not null)
                .Select(card => TryBuildPayment(CreditCardType, card.Name, card.NextInvoiceDueDate!.Value, today))
                .Where(payment => payment is not null)
                .Select(payment => payment!)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load credit cards for the payments-due aggregation.");
            return [];
        }
    }

    private static PaymentDueDTO? TryBuildMensaisPayment(RecurringBill bill, DateOnly today)
    {
        if (bill.DueDay < RecurringBill.MinDueDay || bill.DueDay > RecurringBill.MaxDueDay)
        {
            return null;
        }

        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        var dueDate = new DateOnly(today.Year, today.Month, Math.Min(bill.DueDay, daysInMonth));

        return TryBuildPayment(MensaisType, bill.Description, dueDate, today);
    }

    private static PaymentDueDTO? TryBuildPayment(string type, string name, DateOnly dueDate, DateOnly today)
    {
        var daysRemaining = dueDate.DayNumber - today.DayNumber;
        if (daysRemaining < 0 || daysRemaining > NotificationWindowDays)
        {
            return null;
        }

        return new PaymentDueDTO
        {
            Type = type,
            Name = name,
            DueDate = dueDate,
            DaysRemaining = daysRemaining
        };
    }

    private static int TypeSortOrder(string type) => type == MensaisType ? 0 : 1;

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(PaymentsDueService), operationName, EntityType);
    }
}
