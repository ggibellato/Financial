using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.Services;

public sealed class MensaisService : IMensaisService
{
    private const int MinDueDay = 1;
    private const int MaxDueDay = 31;

    private readonly ICashFlowRepository _repository;

    public MensaisService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<RecurringBillDTO> CreateBillAsync(CreateRecurringBillDTO request)
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

        return ToDto(bill);
    }

    public async Task DeleteBillAsync(Guid id)
    {
        _ = _repository.GetRecurringBills().FirstOrDefault(b => b.Id == id)
            ?? throw new KeyNotFoundException($"Recurring bill '{id}' was not found.");

        _repository.DeleteRecurringBill(id);
        await _repository.SaveChangesAsync().ConfigureAwait(false);
    }

    public IReadOnlyList<RecurringBillDTO> GetBills() =>
        _repository.GetRecurringBills().Select(ToDto).ToList();

    public async Task<RecurringBillDTO> UpdateBillAsync(Guid id, UpdateRecurringBillDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bill = _repository.GetRecurringBills().FirstOrDefault(b => b.Id == id)
            ?? throw new KeyNotFoundException($"Recurring bill '{id}' was not found.");

        if (!BillStatusParser.TryParse(request.Status, out var status))
        {
            throw new ArgumentException($"Status '{request.Status}' is not recognized.");
        }

        bill.Update(status, request.Value);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(bill);
    }

    public async Task<IReadOnlyList<RecurringBillDTO>> ResetAllToUnsetAsync()
    {
        var bills = _repository.GetRecurringBills().ToList();
        foreach (var bill in bills)
        {
            bill.ResetToUnset();
        }

        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return bills.Select(ToDto).ToList();
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
