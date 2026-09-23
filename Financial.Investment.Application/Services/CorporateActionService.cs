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
                var method = AssetMutationHelper.ResolveCostBasisMethod(_repository, request.BrokerName);
                var investments = _repository.GetInvestments();

                // Validated before any mutation: a target-name collision or a missing existing target
                // must leave the document untouched.
                EnsureTargetIsAvailable(portfolio, request);

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
                    targetAsset = ResolveOrCreateTargetAsset(portfolio, request, out var isNewTarget);
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
                    ca => ca.CorrelationId == previousSourceRecord.CorrelationId && ca.Role == CorporateAction.MergerRole.Target)
                    ?? throw new InvalidOperationException($"Corporate action {previousSourceRecord.Id} has no linked target record.");

                var brokerCurrency = ParseBrokerCurrency(broker);
                var method = AssetMutationHelper.ResolveCostBasisMethod(_repository, request.BrokerName);
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

    private static void EnsureTargetIsAvailable(Portfolio portfolio, CorporateActionMergerCreateDTO request)
    {
        var existing = portfolio.FindAsset(request.TargetAssetName);

        if (request.CreateTargetAssetInline && existing is not null)
        {
            throw new InvestmentRuleViolationException(
                $"Portfolio \"{portfolio.Name}\" already has an asset named \"{request.TargetAssetName}\".");
        }

        if (!request.CreateTargetAssetInline && existing is null)
        {
            throw new KeyNotFoundException($"Asset \"{request.TargetAssetName}\" was not found in portfolio \"{portfolio.Name}\".");
        }
    }

    private static Asset ResolveOrCreateTargetAsset(Portfolio portfolio, CorporateActionMergerCreateDTO request, out bool isNewTarget)
    {
        if (!request.CreateTargetAssetInline)
        {
            isNewTarget = false;
            return portfolio.FindAsset(request.TargetAssetName)
                ?? throw new KeyNotFoundException($"Asset \"{request.TargetAssetName}\" was not found in portfolio \"{portfolio.Name}\".");
        }

        var assetClass = request.TargetClass
            ?? GlobalAssetClassMapping.Resolve(request.TargetCountry ?? CountryCode.Unknown, request.TargetLocalTypeCode ?? string.Empty);

        isNewTarget = true;
        return Asset.Create(
            request.TargetAssetName,
            request.TargetISIN ?? string.Empty,
            request.TargetExchange ?? string.Empty,
            request.TargetTicker ?? string.Empty,
            request.TargetCountry ?? CountryCode.Unknown,
            request.TargetLocalTypeCode ?? string.Empty,
            assetClass);
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
