using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class MensaisService : IMensaisService
{
    private const int MinDueDay = 1;
    private const int MaxDueDay = 31;
    private const string EntityType = "RecurringBill";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<MensaisService> _logger;

    public MensaisService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<MensaisService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RecurringBillDTO> CreateBillAsync(CreateRecurringBillDTO request)
    {
        using var span = StartSpan("CreateBill");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.DueDay < MinDueDay || request.DueDay > MaxDueDay)
            {
                throw new ArgumentException($"Due day must be between {MinDueDay} and {MaxDueDay}.");
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                throw new ArgumentException("Description is required.");
            }

            if (!AreaParser.TryParse(request.Area, out var area))
            {
                throw new ArgumentException($"Area '{request.Area}' is not recognized.");
            }

            // NitNumber/MinimumWageValue are INSS-specific and only ever populated by the
            // spreadsheet import (which builds RecurringBill directly); bills added here start without them.
            var bill = RecurringBill.Create(
                request.DueDay, request.Description, request.Value, area, request.Note, nitNumber: null, minimumWageValue: null);

            _repository.AddRecurringBill(bill);
            await _repository.SaveChangesAsync().ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.EntityId, bill.Id.ToString());
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "CreateBill");
            return ToDto(bill);
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    public async Task DeleteBillAsync(Guid id)
    {
        using var span = StartSpan("DeleteBill");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            _ = _repository.GetRecurringBills().FirstOrThrow(b => b.Id == id, "Recurring bill", id);

            _repository.DeleteRecurringBill(id);
            await _repository.SaveChangesAsync().ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "DeleteBill");
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    public IReadOnlyList<RecurringBillDTO> GetBills()
    {
        using var span = StartSpan("GetBills");
        try
        {
            var result = _repository.GetRecurringBills().Select(ToDto).ToList();

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "GetBills");
            return result;
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    public async Task<RecurringBillDTO> UpdateBillAsync(Guid id, UpdateRecurringBillDTO request)
    {
        using var span = StartSpan("UpdateBill");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var bill = _repository.GetRecurringBills().FirstOrThrow(b => b.Id == id, "Recurring bill", id);

            if (!BillStatusParser.TryParse(request.Status, out var status))
            {
                throw new ArgumentException($"Status '{request.Status}' is not recognized.");
            }

            bill.Update(status, request.Value);
            await _repository.SaveChangesAsync().ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "UpdateBill");
            return ToDto(bill);
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    public async Task<IReadOnlyList<RecurringBillDTO>> ResetAllToUnsetAsync()
    {
        using var span = StartSpan("ResetAllToUnset");
        try
        {
            var bills = _repository.GetRecurringBills().ToList();
            foreach (var bill in bills)
            {
                bill.ResetToUnset();
            }

            await _repository.SaveChangesAsync().ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "ResetAllToUnset");
            return bills.Select(ToDto).ToList();
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        var span = _tracer.StartSpan($"CashFlow.MensaisService.{operationName}");
        span.SetAttribute(TelemetryAttributeKeys.BoundedContext, "CashFlow");
        span.SetAttribute(TelemetryAttributeKeys.EntityType, EntityType);
        span.SetAttribute(TelemetryAttributeKeys.OperationName, operationName);
        return span;
    }

    private static RecurringBillDTO ToDto(RecurringBill bill) => new()
    {
        Id = bill.Id,
        DueDay = bill.DueDay,
        Description = bill.Description,
        Value = bill.Value,
        Area = bill.Area.ToString(),
        Note = bill.Note,
        NitNumber = bill.NitNumber,
        MinimumWageValue = bill.MinimumWageValue,
        Status = bill.Status.ToString()
    };
}
