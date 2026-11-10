using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Time;
using Financial.Shared.Abstractions.Validation;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class AccountAdminService : IAccountAdminService
{
    private const string EntityType = "Account";
    private const string DisplayName = "An account";

    private readonly ICashFlowRepository _repository;
    private readonly ICreditCardCalendarSyncService _calendarSyncTrigger;
    private readonly TimeProvider _timeProvider;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<AccountAdminService> _logger;

    public AccountAdminService(
        ICashFlowRepository repository,
        ICreditCardCalendarSyncService calendarSyncTrigger,
        TimeProvider timeProvider,
        ITelemetryTracer tracer,
        ILogger<AccountAdminService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _calendarSyncTrigger = calendarSyncTrigger ?? throw new ArgumentNullException(nameof(calendarSyncTrigger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<AccountTemplateDTO> GetTemplates()
    {
        using var span = StartSpan("GetAccountTemplates");
        try
        {
            var result = AccountTemplates.Available.Select(ToTemplateDto).ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetAccountTemplates");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<AccountDTO> CreateAsync(AccountCreateDTO request)
    {
        using var span = StartSpan("CreateAccount");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var currency = CurrencyParser.Parse(request.Currency);
            var account = Account.Create(request.Name, ParseTemplate(request.TemplateName), currency);
            _repository.GetAccounts().EnsureAccountNameIsUnique(account.Name, null, DisplayName);
            var currencies = RequestedCurrencies(account.Has, account.Name, currency, request.Currencies);
            EnsureOtherParticipantFits(account.Has, account.Name, request.OtherParticipant);
            EnsureCapabilityFieldsArePresent(account.Has, request.OpeningBalance, request.BalanceStartDate, request.RoundUpEnabled);
            var category = ResolveCategory(account.Has, account.Name, request.CategoryId, currentCategory: null);
            account.ApplyCurrencies(currencies);
            ApplyOtherParticipant(account, request.OtherParticipant);
            ApplyCapabilityFields(account, request.OpeningBalance, request.BalanceStartDate, request.RoundUpEnabled);
            ApplyCategory(account, category);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.AddAccount(account);
                return true;
            }).ConfigureAwait(false);

            TriggerCalendarSyncIfCard(account, wasCard: false);

            span.SetAttribute(TelemetryAttributeKeys.EntityId, account.Id.ToString());
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "CreateAccount");
            return AccountQueryService.ToAccountDto(account, hasRecords: false);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<AccountDTO> UpdateAsync(Guid id, AccountUpdateDTO request)
    {
        using var span = StartSpan("UpdateAccount");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var account = FindAccountOrThrow(id);
            var template = ParseTemplate(request.TemplateName);
            var currency = CurrencyParser.Parse(request.Currency);
            var hasRecords = AccountReferenceReader.IsReferenced(_repository, id);
            var trimmedName = request.Name.Trim();
            Func<Capability, bool> templateHas = AccountTemplates.CapabilitiesOf(template).Contains;
            var currencies = RequestedCurrencies(templateHas, trimmedName, currency, request.Currencies);
            var changesSetup = !account.IsSetUpAs(template, currencies);
            var wasCard = account.Has(Capability.HasStatements);
            account.EnsureSetupCanChange(template, currencies, hasRecords);

            _repository.GetAccounts().EnsureAccountNameIsUnique(request.Name, id, DisplayName);
            var targetHas = changesSetup ? templateHas : account.Has;
            EnsureOtherParticipantFits(targetHas, trimmedName, request.OtherParticipant);
            EnsureTemplateFieldsAreValid(trimmedName, template, currencies, request.OtherParticipant);
            EnsureCapabilityFieldsArePresent(targetHas, request.OpeningBalance, request.BalanceStartDate, request.RoundUpEnabled);
            var category = ResolveCategory(targetHas, request.Name, request.CategoryId, account.Category);

            await _repository.ApplyAndSaveAsync(() =>
            {
                account.Update(request.Name);
                account.ChangeSetup(template, currencies, hasRecords);
                ApplyOtherParticipant(account, request.OtherParticipant);
                ApplyCapabilityFields(account, request.OpeningBalance, request.BalanceStartDate, request.RoundUpEnabled);
                ApplyCategory(account, category);
                account.SetActive(request.IsActive);
                return true;
            }).ConfigureAwait(false);

            TriggerCalendarSyncIfCard(account, wasCard);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateAccount");
            return AccountQueryService.ToAccountDto(account, hasRecords);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        using var span = StartSpan("DeleteAccount");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            var account = FindAccountOrThrow(id);

            if (AccountReferenceReader.IsReferenced(_repository, id))
            {
                throw new EntityInUseException($"{account.Name} has transactions and cannot be deleted. Deactivate it instead.");
            }

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.DeleteAccount(id);
                return true;
            }).ConfigureAwait(false);

            TriggerCalendarSyncIfCard(account, wasCard: false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeleteAccount");
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private void EnsureCapabilityFieldsArePresent(
        Func<Capability, bool> has, decimal? openingBalance, DateOnly? balanceStartDate, bool? roundUpEnabled)
    {
        if (has(Capability.HasOpeningBalance))
        {
            if (openingBalance is null)
            {
                throw new ArgumentException("Opening balance is required.");
            }

            if (balanceStartDate is not { } startDate)
            {
                throw new ArgumentException("Balance start date is required.");
            }

            if (startDate > _timeProvider.GetLocalToday())
            {
                throw new ArgumentException("Balance start date can't be in the future.");
            }
        }

        if (has(Capability.SupportsRoundUp) && roundUpEnabled is null)
        {
            throw new ArgumentException("Round-up is required.");
        }
    }

    private static IReadOnlyList<Currency> RequestedCurrencies(
        Func<Capability, bool> has, string accountName, Currency currency, IReadOnlyList<string>? codes)
    {
        if (!has(Capability.MultiCurrency) && (codes is null || codes.Select(CurrencyParser.Parse).Distinct().SequenceEqual([currency])))
        {
            return [currency];
        }

        Account.EnsureMultiCurrency(has, accountName);
        var requested = Account.ChosenCurrencies((codes ?? []).Select(CurrencyParser.Parse));

        return requested.Contains(currency)
            ? requested
            : throw new ArgumentException("Currency must be one of the account's currencies.");
    }

    private static void EnsureOtherParticipantFits(Func<Capability, bool> has, string accountName, string? otherParticipant)
    {
        if (otherParticipant is not null)
        {
            Account.EnsureHasParticipants(has, accountName);
        }
    }

    private static void EnsureTemplateFieldsAreValid(
        string accountName, AccountTemplate template, IReadOnlyList<Currency> currencies, string? otherParticipant)
    {
        var preview = Account.Create(accountName, template, currencies[0]);
        preview.ApplyCurrencies(currencies);
        ApplyOtherParticipant(preview, otherParticipant);
    }

    private static void ApplyOtherParticipant(Account account, string? otherParticipant)
    {
        if (account.Has(Capability.HasParticipants))
        {
            account.SetOtherParticipant(otherParticipant ?? string.Empty);
        }
    }

    private static void ApplyCapabilityFields(Account account, decimal? openingBalance, DateOnly? balanceStartDate, bool? roundUpEnabled)
    {
        if (openingBalance is not null || balanceStartDate is not null)
        {
            account.SetOpeningBalance(openingBalance.GetValueOrDefault(), balanceStartDate.GetValueOrDefault());
        }

        if (roundUpEnabled is { } enabled)
        {
            account.SetRoundUp(enabled);
        }
    }

    private Category? ResolveCategory(Func<Capability, bool> has, string accountName, Guid? categoryId, Category? currentCategory)
    {
        if (categoryId is null && !has(Capability.HasPots))
        {
            return null;
        }

        Account.EnsureHasCategorySetting(has, accountName);
        if (categoryId is not { } id)
        {
            throw new ArgumentException("Choose the category for this account's bank expenses.");
        }

        return currentCategory?.Id == id
            ? currentCategory
            : _repository.GetCategories().FirstOrDefault(category => category.Id == id && category.Active && !category.IsInvestment)
                ?? throw new ArgumentException($"Category '{id}' is not recognized.");
    }

    private static void ApplyCategory(Account account, Category? category)
    {
        if (category is not null)
        {
            account.SetCategory(category);
        }
    }

    private static AccountTemplate ParseTemplate(string? name) =>
        EnumParser.TryParseEnum<AccountTemplate>(name, out var template) && AccountTemplates.Available.Contains(template)
            ? template
            : throw new ArgumentException($"Unknown account template '{name}'.");

    private static AccountTemplateDTO ToTemplateDto(AccountTemplate template)
    {
        return new AccountTemplateDTO
        {
            Name = template.ToString(),
            Currencies = AccountTemplates.AllowedCurrencies(template).Select(currency => currency.ToString()).ToList(),
            Capabilities = AccountQueryService.CapabilityNames(AccountTemplates.CapabilitiesOf(template).Contains)
        };
    }

    private void TriggerCalendarSyncIfCard(Account account, bool wasCard)
    {
        if (wasCard || account.Has(Capability.HasStatements))
        {
            _calendarSyncTrigger.TriggerSync(account.Id);
        }
    }

    private Account FindAccountOrThrow(Guid id) =>
        _repository.GetAccounts().FirstOrThrow(account => account.Id == id, EntityType, id);

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(AccountAdminService), operationName, EntityType);
    }
}
