using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Validation;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Exceptions;
using Financial.Investment.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

public sealed class CorporateActionService : ICorporateActionService
{
    private const string EntityType = "CorporateAction";

    private readonly IInvestmentRepository _repository;
    private readonly INavigationService _navigationService;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<CorporateActionService> _logger;

    public CorporateActionService(
        IInvestmentRepository repository,
        INavigationService navigationService,
        ITelemetryTracer tracer,
        ILogger<CorporateActionService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AssetDetailsDTO?> AddSplitAsync(CorporateActionSplitCreateDTO request)
    {
        using var span = StartSpan("AddSplit");
        try
        {
            var result = await AssetMutationHelper.ExecuteAssetMutationAsync(
                _repository,
                _navigationService,
                request.BrokerName,
                request.PortfolioName,
                request.AssetName,
                asset =>
                {
                    var corporateAction = CorporateAction.CreateSplit(request.EffectiveDate, request.RatioFactor, request.Note);
                    var method = ResolveCostBasisMethod(request.BrokerName);
                    asset.RecordCorporateAction(corporateAction, method, _repository.GetInvestments());
                    return true;
                }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "AddSplit");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<AssetDetailsDTO?> UpdateSplitAsync(CorporateActionSplitUpdateDTO request)
    {
        using var span = StartSpan("UpdateSplit");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, request.Id.ToString());
        try
        {
            if (request.Id == Guid.Empty)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "UpdateSplit");
                return null;
            }

            var result = await AssetMutationHelper.ExecuteAssetMutationAsync(
                _repository,
                _navigationService,
                request.BrokerName,
                request.PortfolioName,
                request.AssetName,
                asset =>
                {
                    var updatedCorporateAction = CorporateAction.CreateSplitWithId(request.Id, request.EffectiveDate, request.RatioFactor, request.Note);
                    var method = ResolveCostBasisMethod(request.BrokerName);
                    return asset.ReviseCorporateAction(updatedCorporateAction, method, _repository.GetInvestments());
                }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateSplit");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<CorporateActionMergerResultDTO?> AddMergerAsync(CorporateActionMergerCreateDTO request)
    {
        using var span = StartSpan("AddMerger");
        try
        {
            if (AssetContextValidator.IsInvalid(request.BrokerName, request.PortfolioName, request.SourceAssetName) ||
                string.IsNullOrWhiteSpace(request.TargetAssetName))
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "AddMerger");
                return null;
            }

            Asset? sourceAsset = null;
            Asset? targetAsset = null;

            await _repository.ApplyAndSaveAsync(() =>
            {
                var broker = ResolveActiveBroker(request.BrokerName);
                var portfolio = ResolvePortfolio(broker, request.PortfolioName);
                sourceAsset = portfolio.FindAsset(request.SourceAssetName)
                    ?? throw new KeyNotFoundException($"Asset \"{request.SourceAssetName}\" was not found in portfolio \"{request.PortfolioName}\".");

                var brokerCurrency = ParseBrokerCurrency(broker);
                var method = broker.CostBasisMethod;
                var investments = _repository.GetInvestments();

                var correlationId = Guid.NewGuid();
                var (sourceQuantity, sourceAveragePrice) = sourceAsset.PositionAsOf(request.EffectiveDate);
                var carriedCostBasis = sourceQuantity * sourceAveragePrice;
                var convertedQuantity = sourceQuantity * request.ExchangeRatio;

                var sourceRecord = CorporateAction.CreateMergerSource(
                    request.EffectiveDate, request.ExchangeRatio, request.CashInLieuAmount, request.Note,
                    correlationId, request.TargetAssetName, convertedQuantity, carriedCostBasis);
                sourceAsset.RecordCorporateAction(sourceRecord, method, investments);

                try
                {
                    // Resolves (or builds, unregistered) the target so nothing needs a compensating
                    // "un-register" if RecordCorporateAction below throws - Portfolio.RegisterAsset only
                    // runs once the target's own record has been recorded successfully.
                    targetAsset = ResolveOrCreateLinkedAsset(
                        portfolio, request.TargetAssetName, request.CreateTargetAssetInline,
                        request.TargetISIN, request.TargetExchange, request.TargetTicker,
                        request.TargetCountry, request.TargetLocalTypeCode, request.TargetClass,
                        out var isNewTarget);
                    var targetRecord = CorporateAction.CreateMergerTarget(
                        request.EffectiveDate, request.Note, correlationId, sourceAsset.Name, convertedQuantity, carriedCostBasis);
                    targetAsset.RecordCorporateAction(targetRecord, method, investments, brokerCurrency);

                    if (isNewTarget)
                    {
                        portfolio.RegisterAsset(targetAsset);
                    }
                }
                catch
                {
                    sourceAsset.RetractCorporateAction(sourceRecord.Id, method, investments);
                    throw;
                }

                return true;
            }).ConfigureAwait(false);

            var result = new CorporateActionMergerResultDTO
            {
                Source = _navigationService.GetAssetDetails(request.BrokerName, request.PortfolioName, sourceAsset!.Name),
                Target = _navigationService.GetAssetDetails(request.BrokerName, request.PortfolioName, targetAsset!.Name)
            };

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "AddMerger");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<CorporateActionMergerResultDTO?> UpdateMergerAsync(CorporateActionMergerUpdateDTO request)
    {
        using var span = StartSpan("UpdateMerger");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, request.Id.ToString());
        try
        {
            if (request.Id == Guid.Empty || AssetContextValidator.IsInvalid(request.BrokerName, request.PortfolioName, request.SourceAssetName))
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "UpdateMerger");
                return null;
            }

            Asset? sourceAsset = null;
            Asset? targetAsset = null;
            var found = false;

            await _repository.ApplyAndSaveAsync(() =>
            {
                var broker = ResolveActiveBroker(request.BrokerName);
                var portfolio = ResolvePortfolio(broker, request.PortfolioName);
                sourceAsset = portfolio.FindAsset(request.SourceAssetName)
                    ?? throw new KeyNotFoundException($"Asset \"{request.SourceAssetName}\" was not found in portfolio \"{request.PortfolioName}\".");

                var previousSourceRecord = sourceAsset.CorporateActions.FirstOrDefault(ca => ca.Id == request.Id);
                if (previousSourceRecord is null)
                {
                    return false;
                }

                found = true;

                targetAsset = portfolio.FindAsset(previousSourceRecord.LinkedAssetName!)
                    ?? throw new KeyNotFoundException($"Asset \"{previousSourceRecord.LinkedAssetName}\" was not found in portfolio \"{request.PortfolioName}\".");
                var previousTargetRecord = targetAsset.CorporateActions.FirstOrDefault(
                    ca => ca.CorrelationId == previousSourceRecord.CorrelationId && ca.Role == CorporateAction.CorporateActionRole.Target)
                    ?? throw new InvalidOperationException($"Corporate action {previousSourceRecord.Id} has no linked target record.");

                var brokerCurrency = ParseBrokerCurrency(broker);
                var method = broker.CostBasisMethod;
                var investments = _repository.GetInvestments();
                var correlationId = previousSourceRecord.CorrelationId!.Value;

                var compensations = new Stack<Action>();
                try
                {
                    sourceAsset.RetractCorporateAction(previousSourceRecord.Id, method, investments);
                    compensations.Push(() => sourceAsset.RecordCorporateAction(previousSourceRecord, method, investments));

                    targetAsset.RetractCorporateAction(previousTargetRecord.Id, method, investments);
                    compensations.Push(() => targetAsset.RecordCorporateAction(previousTargetRecord, method, investments, brokerCurrency));

                    var (quantity, averagePrice) = sourceAsset.PositionAsOf(request.EffectiveDate);
                    var carriedCostBasis = quantity * averagePrice;
                    var convertedQuantity = quantity * request.ExchangeRatio;

                    var newSourceRecord = CorporateAction.CreateMergerSourceWithId(
                        request.Id, request.EffectiveDate, request.ExchangeRatio, request.CashInLieuAmount, request.Note,
                        correlationId, targetAsset.Name, convertedQuantity, carriedCostBasis);
                    sourceAsset.RecordCorporateAction(newSourceRecord, method, investments);
                    compensations.Push(() => sourceAsset.RetractCorporateAction(newSourceRecord.Id, method, investments));

                    var newTargetRecord = CorporateAction.CreateMergerTargetWithId(
                        previousTargetRecord.Id, request.EffectiveDate, request.Note,
                        correlationId, sourceAsset.Name, convertedQuantity, carriedCostBasis);
                    targetAsset.RecordCorporateAction(newTargetRecord, method, investments, brokerCurrency);
                }
                catch
                {
                    while (compensations.Count > 0)
                    {
                        compensations.Pop().Invoke();
                    }

                    throw;
                }

                return true;
            }).ConfigureAwait(false);

            if (!found)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "UpdateMerger");
                return null;
            }

            var result = new CorporateActionMergerResultDTO
            {
                Source = _navigationService.GetAssetDetails(request.BrokerName, request.PortfolioName, sourceAsset!.Name),
                Target = _navigationService.GetAssetDetails(request.BrokerName, request.PortfolioName, targetAsset!.Name)
            };

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateMerger");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<CorporateActionSpinOffResultDTO?> AddSpinOffAsync(CorporateActionSpinOffCreateDTO request)
    {
        using var span = StartSpan("AddSpinOff");
        try
        {
            if (AssetContextValidator.IsInvalid(request.BrokerName, request.PortfolioName, request.ParentAssetName) ||
                string.IsNullOrWhiteSpace(request.NewAssetName))
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "AddSpinOff");
                return null;
            }

            Asset? parentAsset = null;
            Asset? newAsset = null;

            await _repository.ApplyAndSaveAsync(() =>
            {
                var broker = ResolveActiveBroker(request.BrokerName);
                var portfolio = ResolvePortfolio(broker, request.PortfolioName);
                parentAsset = portfolio.FindAsset(request.ParentAssetName)
                    ?? throw new KeyNotFoundException($"Asset \"{request.ParentAssetName}\" was not found in portfolio \"{request.PortfolioName}\".");

                var brokerCurrency = ParseBrokerCurrency(broker);
                var method = broker.CostBasisMethod;
                var investments = _repository.GetInvestments();

                var correlationId = Guid.NewGuid();
                var (quantity, averagePrice) = parentAsset.PositionAsOf(request.EffectiveDate);
                var carriedCostBasis = request.AllocationPercentage / 100m * (quantity * averagePrice);

                var parentRecord = CorporateAction.CreateSpinOffParent(
                    request.EffectiveDate, request.AllocationPercentage, request.Note,
                    correlationId, request.NewAssetName, request.QuantityReceived, carriedCostBasis);
                parentAsset.RecordCorporateAction(parentRecord, method, investments);

                try
                {
                    newAsset = ResolveOrCreateLinkedAsset(
                        portfolio, request.NewAssetName, request.CreateNewAssetInline,
                        request.NewISIN, request.NewExchange, request.NewTicker,
                        request.NewCountry, request.NewLocalTypeCode, request.NewClass,
                        out var isNewAsset);
                    var newRecord = CorporateAction.CreateSpinOffNew(
                        request.EffectiveDate, request.Note, correlationId, parentAsset.Name, request.QuantityReceived, carriedCostBasis);
                    newAsset.RecordCorporateAction(newRecord, method, investments, brokerCurrency);

                    if (isNewAsset)
                    {
                        portfolio.RegisterAsset(newAsset);
                    }
                }
                catch
                {
                    parentAsset.RetractCorporateAction(parentRecord.Id, method, investments);
                    throw;
                }

                return true;
            }).ConfigureAwait(false);

            var result = new CorporateActionSpinOffResultDTO
            {
                Parent = _navigationService.GetAssetDetails(request.BrokerName, request.PortfolioName, parentAsset!.Name),
                New = _navigationService.GetAssetDetails(request.BrokerName, request.PortfolioName, newAsset!.Name)
            };

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "AddSpinOff");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<CorporateActionSpinOffResultDTO?> UpdateSpinOffAsync(CorporateActionSpinOffUpdateDTO request)
    {
        using var span = StartSpan("UpdateSpinOff");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, request.Id.ToString());
        try
        {
            if (request.Id == Guid.Empty || AssetContextValidator.IsInvalid(request.BrokerName, request.PortfolioName, request.ParentAssetName))
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "UpdateSpinOff");
                return null;
            }

            Asset? parentAsset = null;
            Asset? newAsset = null;
            var found = false;

            await _repository.ApplyAndSaveAsync(() =>
            {
                var broker = ResolveActiveBroker(request.BrokerName);
                var portfolio = ResolvePortfolio(broker, request.PortfolioName);
                parentAsset = portfolio.FindAsset(request.ParentAssetName)
                    ?? throw new KeyNotFoundException($"Asset \"{request.ParentAssetName}\" was not found in portfolio \"{request.PortfolioName}\".");

                var previousParentRecord = parentAsset.CorporateActions.FirstOrDefault(ca => ca.Id == request.Id);
                if (previousParentRecord is null)
                {
                    return false;
                }

                found = true;

                newAsset = portfolio.FindAsset(previousParentRecord.LinkedAssetName!)
                    ?? throw new KeyNotFoundException($"Asset \"{previousParentRecord.LinkedAssetName}\" was not found in portfolio \"{request.PortfolioName}\".");
                var previousNewRecord = newAsset.CorporateActions.FirstOrDefault(
                    ca => ca.CorrelationId == previousParentRecord.CorrelationId && ca.Role == CorporateAction.CorporateActionRole.New)
                    ?? throw new InvalidOperationException($"Corporate action {previousParentRecord.Id} has no linked new-asset record.");

                var brokerCurrency = ParseBrokerCurrency(broker);
                var method = broker.CostBasisMethod;
                var investments = _repository.GetInvestments();
                var correlationId = previousParentRecord.CorrelationId!.Value;

                var compensations = new Stack<Action>();
                try
                {
                    parentAsset.RetractCorporateAction(previousParentRecord.Id, method, investments);
                    compensations.Push(() => parentAsset.RecordCorporateAction(previousParentRecord, method, investments));

                    newAsset.RetractCorporateAction(previousNewRecord.Id, method, investments);
                    compensations.Push(() => newAsset.RecordCorporateAction(previousNewRecord, method, investments, brokerCurrency));

                    var (quantity, averagePrice) = parentAsset.PositionAsOf(request.EffectiveDate);
                    var carriedCostBasis = request.AllocationPercentage / 100m * (quantity * averagePrice);

                    var newParentRecord = CorporateAction.CreateSpinOffParentWithId(
                        request.Id, request.EffectiveDate, request.AllocationPercentage, request.Note,
                        correlationId, newAsset.Name, request.QuantityReceived, carriedCostBasis);
                    parentAsset.RecordCorporateAction(newParentRecord, method, investments);
                    compensations.Push(() => parentAsset.RetractCorporateAction(newParentRecord.Id, method, investments));

                    var newNewRecord = CorporateAction.CreateSpinOffNewWithId(
                        previousNewRecord.Id, request.EffectiveDate, request.Note,
                        correlationId, parentAsset.Name, request.QuantityReceived, carriedCostBasis);
                    newAsset.RecordCorporateAction(newNewRecord, method, investments, brokerCurrency);
                }
                catch
                {
                    while (compensations.Count > 0)
                    {
                        compensations.Pop().Invoke();
                    }

                    throw;
                }

                return true;
            }).ConfigureAwait(false);

            if (!found)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "UpdateSpinOff");
                return null;
            }

            var result = new CorporateActionSpinOffResultDTO
            {
                Parent = _navigationService.GetAssetDetails(request.BrokerName, request.PortfolioName, parentAsset!.Name),
                New = _navigationService.GetAssetDetails(request.BrokerName, request.PortfolioName, newAsset!.Name)
            };

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateSpinOff");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<AssetDetailsDTO?> DeleteCorporateActionAsync(CorporateActionDeleteDTO request)
    {
        using var span = StartSpan("DeleteCorporateAction");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, request.Id.ToString());
        try
        {
            if (request.Id == Guid.Empty)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "DeleteCorporateAction");
                return null;
            }

            var result = await AssetMutationHelper.ExecuteAssetMutationAsync(
                _repository,
                _navigationService,
                request.BrokerName,
                request.PortfolioName,
                request.AssetName,
                asset => DeleteCorporateActionFromAsset(asset, request)).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeleteCorporateAction");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private bool DeleteCorporateActionFromAsset(Asset asset, CorporateActionDeleteDTO request)
    {
        var found = asset.CorporateActions.FirstOrDefault(ca => ca.Id == request.Id);
        if (found is null)
        {
            return false;
        }

        var method = ResolveCostBasisMethod(request.BrokerName);
        var investments = _repository.GetInvestments();

        if (found.Type == CorporateAction.CorporateActionType.Split)
        {
            return asset.RetractCorporateAction(found.Id, method, investments);
        }

        var linkedAsset = _repository.GetAsset(request.BrokerName, request.PortfolioName, found.LinkedAssetName!)
            ?? throw new KeyNotFoundException($"Asset \"{found.LinkedAssetName}\" was not found in portfolio \"{request.PortfolioName}\".");
        var linkedRecord = linkedAsset.CorporateActions.FirstOrDefault(ca => ca.CorrelationId == found.CorrelationId && ca.Id != found.Id)
            ?? throw new InvalidOperationException($"Corporate action {found.Id} has no linked record for correlation id {found.CorrelationId}.");

        asset.RetractCorporateAction(found.Id, method, investments);
        try
        {
            linkedAsset.RetractCorporateAction(linkedRecord.Id, method, investments);
        }
        catch
        {
            asset.RecordCorporateAction(found, method, investments, ResolveBrokerCurrency(request.BrokerName));
            throw;
        }

        return true;
    }

    private Broker ResolveActiveBroker(string brokerName) =>
        _repository.GetBrokerList(InvestmentScope.Active).FirstOrDefault(b => string.Equals(b.Name, brokerName, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Active broker \"{brokerName}\" was not found.");

    private static Portfolio ResolvePortfolio(Broker broker, string portfolioName) =>
        broker.FindPortfolio(portfolioName)
            ?? throw new KeyNotFoundException($"Portfolio \"{portfolioName}\" was not found under broker \"{broker.Name}\".");

    private static Asset ResolveOrCreateLinkedAsset(
        Portfolio portfolio,
        string assetName,
        bool createInline,
        string? isin,
        string? exchange,
        string? ticker,
        CountryCode? country,
        string? localTypeCode,
        GlobalAssetClass? assetClass,
        out bool isNew)
    {
        var existing = portfolio.FindAsset(assetName);

        if (!createInline)
        {
            isNew = false;
            return existing ?? throw new KeyNotFoundException($"Asset \"{assetName}\" was not found in portfolio \"{portfolio.Name}\".");
        }

        if (existing is not null)
        {
            throw new InvestmentRuleViolationException(
                $"An asset named \"{assetName}\" already exists in portfolio \"{portfolio.Name}\".");
        }

        var resolvedCountry = country ?? CountryCode.Unknown;
        var resolvedAssetClass = assetClass ?? GlobalAssetClassMapping.Resolve(resolvedCountry, localTypeCode ?? string.Empty);

        isNew = true;
        return Asset.Create(
            assetName,
            isin ?? string.Empty,
            exchange ?? string.Empty,
            ticker ?? string.Empty,
            resolvedCountry,
            localTypeCode ?? string.Empty,
            resolvedAssetClass);
    }

    private static Currency ParseBrokerCurrency(Broker broker)
    {
        if (!EnumParser.TryParseEnum<Currency>(broker.Currency, out var currency))
        {
            throw new ArgumentException($"Broker \"{broker.Name}\" has an unrecognized currency \"{broker.Currency}\".");
        }

        return currency;
    }

    private Currency ResolveBrokerCurrency(string brokerName) => ParseBrokerCurrency(ResolveActiveBroker(brokerName));

    private CostBasisMethod ResolveCostBasisMethod(string brokerName) =>
        AssetMutationHelper.ResolveCostBasisMethod(_repository, brokerName);

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("Investment", nameof(CorporateActionService), operationName, EntityType);
    }
}
