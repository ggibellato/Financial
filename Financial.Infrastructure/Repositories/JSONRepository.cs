using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Financial.Infrastructure.Repositories;

public sealed class JSONRepository : IRepository
{
    private readonly IJsonStorage _storage;
    private readonly Investments _investiments;

    public JSONRepository(IJsonStorage storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _investiments = LoadInvestments(_storage);
    }

    public List<string> GetAllAssetsFullName()
    {
        return _investiments.Brokers
            .SelectMany(b => b.Portfolios.SelectMany(p => p.Assets.Select(a => $"{b.Name}/{p.Name}/{a.Name}")))
            .ToList();
    }

    public IEnumerable<Asset> GetAssetsByBroker(string name)
    {
        return GetAssetsByBrokerInternal(name);
    }

    public IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio)
    {
        return GetPortfoliosByBroker(broker)
            .Where(p => p.Name == portfolio)
            .SelectMany(p => p.Assets);
    }

    public IEnumerable<Asset> GetAssetsByPortfolio(string name)
    {
        return GetPortfoliosByName(name)
            .SelectMany(p => p.Assets);
    }

    public IEnumerable<Asset> GetAssetsByAssetName(string name)
    {
        return GetAllAssetsInternal()
            .Where(a => a.Name == name);
    }

    public Asset? GetAsset(string brokerName, string portfolioName, string assetName)
    {
        return GetAssetsByBrokerPortfolio(brokerName, portfolioName)
            .FirstOrDefault(a => a.Name == assetName);
    }

    public IEnumerable<Broker> GetBrokerList()
    {
        return _investiments.Brokers;
    }

    public BrokerInfoDTO GetBrokerInfo(string brokerName)
    {
        var ret = new BrokerInfoDTO();
        ret.TotalBought = GetTotalBoughtByBroker(brokerName, false);
        ret.TotalSold = GetTotalSoldByBroker(brokerName, false);
        ret.TotalCredits = GetTotalCreditsByBroker(brokerName, false);

        ret.TotalBoughtActive = GetTotalBoughtByBroker(brokerName, true);
        ret.TotalSoldActive = GetTotalSoldByBroker(brokerName, true);
        ret.TotalCreditsActive = GetTotalCreditsByBroker(brokerName, true);

        ret.PortfoliosActive = GetPortfolioAssetsByBroker(brokerName, true);
        ret.PortfoliosInactive = GetPortfolioAssetsByBroker(brokerName, false);
        return ret;
    }

    public AssetInfoDTO GetAssetInfo(string brokerName, string portfolio, string assetName)
    {
        var asset = GetBrokerOrThrow(brokerName)
            .Portfolios.First(p => p.Name == portfolio)
            .Assets.First(a => a.Name == assetName);

        return new AssetInfoDTO
        {
            Exchange = asset.Exchange,
            Ticker = asset.Ticker,
            Country = asset.Country,
            LocalTypeCode = asset.LocalTypeCode,
            Class = asset.Class,
            Quantity = asset.Quantity,
            AvaragePrice = asset.AvargePrice,
            TotalBought = SumOperationsByType(asset.Operations, Operation.OperationType.Buy),
            TotalSold = SumOperationsByType(asset.Operations, Operation.OperationType.Sell),
            Credits = BuildCreditInfo(asset.Credits),
            InvestedHistory = BuildInvestedHistory(asset.Operations)
        };
    }

    public void SaveChanges()
    {
        var json = _investiments.Serialize();
        _storage.WriteAsync(json)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    private static Investments LoadInvestments(IJsonStorage storage)
    {
        var json = storage.ReadAsync()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        return Investments.Deserialize(json);
    }

    private IEnumerable<Broker> GetBrokersByName(string brokerName) =>
        _investiments.Brokers.Where(b => b.Name == brokerName);

    private Broker GetBrokerOrThrow(string brokerName) =>
        _investiments.Brokers.First(b => b.Name == brokerName);

    private IEnumerable<Portfolio> GetPortfoliosByBroker(string brokerName) =>
        GetBrokersByName(brokerName).SelectMany(b => b.Portfolios);

    private IEnumerable<Asset> GetAssetsByBrokerInternal(string brokerName) =>
        GetPortfoliosByBroker(brokerName).SelectMany(p => p.Assets);

    private IEnumerable<Portfolio> GetPortfoliosByName(string portfolioName) =>
        _investiments.Brokers.SelectMany(b => b.Portfolios.Where(p => p.Name == portfolioName));

    private IEnumerable<Asset> GetAllAssetsInternal() =>
        _investiments.Brokers.SelectMany(b => b.Portfolios.SelectMany(p => p.Assets));

    private static IEnumerable<Asset> FilterActiveAssets(IEnumerable<Asset> assets, bool activeOnly) =>
        activeOnly ? assets.Where(a => a.Active) : assets;

    private IEnumerable<TItem> GetAssetItemsByBroker<TItem>(
        string brokerName,
        bool activeOnly,
        Func<Asset, IEnumerable<TItem>> selector) =>
        FilterActiveAssets(GetAssetsByBrokerInternal(brokerName), activeOnly)
            .SelectMany(selector);

    private IEnumerable<Operation> GetOperationsByBroker(string brokerName, bool activeOnly) =>
        GetAssetItemsByBroker(brokerName, activeOnly, asset => asset.Operations);

    private IEnumerable<Credit> GetCreditsByBroker(string brokerName, bool activeOnly) =>
        GetAssetItemsByBroker(brokerName, activeOnly, asset => asset.Credits);

    private static CreditInfoDTO BuildCreditInfo(IEnumerable<Credit> credits)
    {
        var creditsInfo = new CreditInfoDTO();
        creditsInfo.Total = credits.Sum(o => o.Value);
        creditsInfo.CreditsByMonth = credits
            .GroupBy(c => new DateOnly(c.Date.Year, c.Date.Month, 1))
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Value));
        return creditsInfo;
    }

    private decimal GetTotalBoughtByBroker(string brokerName, bool active)
    {
        return GetTotalOperationsByBroker(brokerName, active, Operation.OperationType.Buy);
    }

    private decimal GetTotalSoldByBroker(string brokerName, bool active)
    {
        return GetTotalOperationsByBroker(brokerName, active, Operation.OperationType.Sell);
    }

    private CreditInfoDTO GetTotalCreditsByBroker(string brokerName, bool active)
    {
        return BuildCreditInfo(GetCreditsByBroker(brokerName, active));
    }

    private decimal GetTotalOperationsByBroker(string brokerName, bool active, Operation.OperationType type)
    {
        return SumOperationsByType(GetOperationsByBroker(brokerName, active), type);
    }

    private static decimal SumOperationsByType(IEnumerable<Operation> operations, Operation.OperationType type)
    {
        return operations
            .Where(o => o.Type == type)
            .Sum(o => o.TotalPrice);
    }

    private static Dictionary<DateOnly, decimal> BuildInvestedHistory(IEnumerable<Operation> operations)
    {
        var history = new Dictionary<DateOnly, decimal>();
        decimal currentValue = 0;
        foreach (var operation in operations.OrderBy(o => o.Date))
        {
            var key = new DateOnly(operation.Date.Year, operation.Date.Month, 1);
            currentValue += operation.TotalPrice * (operation.Type == Operation.OperationType.Buy ? 1 : -1);
            history[key] = currentValue;
        }

        return history;
    }

    private List<PortfolioDTO> GetPortfolioAssetsByBroker(string brokerName, bool active)
    {
        var ret = new List<PortfolioDTO>();
        var broker = GetBrokerOrThrow(brokerName);
        foreach (var p in broker.Portfolios)
        {
            var assets = p.Assets.Where(a => a.Active == active);
            if (assets.Any())
            {
                var pDTO = new PortfolioDTO
                {
                    Name = p.Name,
                    Assets = assets.Select(a => a.Name).ToList()
                };
                ret.Add(pDTO);
            }
        }
        return ret;
    }
}
