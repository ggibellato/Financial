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

    public async Task<RecurringBillTemplateDTO> CreateTemplateAsync(CreateRecurringBillTemplateDTO request)
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

        var template = RecurringBillTemplate.Create(
            request.DueDay, request.Description, request.Value, area, request.Note, request.NitNumber, request.MinimumWageValue);

        _repository.AddRecurringBillTemplate(template);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToTemplateDto(template);
    }

    public IReadOnlyList<RecurringBillTemplateDTO> GetTemplates() =>
        _repository.GetRecurringBillTemplates().Select(ToTemplateDto).ToList();

    public async Task<IReadOnlyList<RecurringBillInstanceDTO>> GetInstancesForMonthAsync(int year, int month)
    {
        var templates = _repository.GetRecurringBillTemplates().Where(t => t.IsActive).ToList();
        var existingInstances = _repository.GetRecurringBillInstances()
            .Where(i => i.Year == year && i.Month == month)
            .ToList();

        var created = false;
        foreach (var template in templates)
        {
            if (existingInstances.Any(i => i.TemplateId == template.Id))
            {
                continue;
            }

            var instance = RecurringBillInstance.Create(template.Id, year, month, template.Value);
            _repository.AddRecurringBillInstance(instance);
            existingInstances.Add(instance);
            created = true;
        }

        if (created)
        {
            await _repository.SaveChangesAsync().ConfigureAwait(false);
        }

        var templatesById = _repository.GetRecurringBillTemplates().ToDictionary(t => t.Id);

        return existingInstances
            .Where(i => templatesById.ContainsKey(i.TemplateId))
            .Select(i => ToInstanceDto(i, templatesById[i.TemplateId]))
            .ToList();
    }

    public async Task<RecurringBillInstanceDTO> UpdateInstanceAsync(Guid id, UpdateRecurringBillInstanceDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var instance = _repository.GetRecurringBillInstances().FirstOrDefault(i => i.Id == id)
            ?? throw new KeyNotFoundException($"Recurring bill instance '{id}' was not found.");

        if (!BillStatusParser.TryParse(request.Status, out var status))
        {
            throw new ArgumentException($"Status '{request.Status}' is not recognized.");
        }

        var template = _repository.GetRecurringBillTemplates().FirstOrDefault(t => t.Id == instance.TemplateId)
            ?? throw new KeyNotFoundException($"Template '{instance.TemplateId}' was not found.");

        instance.Update(status, request.Value);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToInstanceDto(instance, template);
    }

    private static RecurringBillTemplateDTO ToTemplateDto(RecurringBillTemplate template) => new()
    {
        Id = template.Id,
        DueDay = template.DueDay,
        Description = template.Description,
        Value = template.Value,
        Area = template.Area.ToString(),
        Note = template.Note,
        NitNumber = template.NitNumber,
        MinimumWageValue = template.MinimumWageValue,
        IsActive = template.IsActive
    };

    private static RecurringBillInstanceDTO ToInstanceDto(RecurringBillInstance instance, RecurringBillTemplate template) => new()
    {
        Id = instance.Id,
        TemplateId = instance.TemplateId,
        Year = instance.Year,
        Month = instance.Month,
        DueDay = template.DueDay,
        Description = template.Description,
        Area = template.Area.ToString(),
        Note = template.Note,
        NitNumber = template.NitNumber,
        MinimumWageValue = template.MinimumWageValue,
        Value = instance.Value,
        Status = instance.Status.ToString()
    };
}
