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
        var result = new List<string>();
        _investiments.Brokers.ToList().ForEach(b =>
        {
            b.Portfolios.ToList().ForEach(p =>
            {
                p.Assets.ToList().ForEach(a =>
                {
                    result.Add($"{b.Name}/{p.Name}/{a.Name}");
                });
            });
        });
        return result;
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
        var ret = new AssetInfoDTO();
        var asset = GetBrokerOrThrow(brokerName)
            .Portfolios.First(p => p.Name == portfolio)
            .Assets.First(a => a.Name == assetName);

        ret.Exchange = asset.Exchange;
        ret.Ticker = asset.Ticker;
        ret.Quantity = asset.Quantity;
        ret.AvaragePrice = asset.AvargePrice;
        ret.TotalBought = asset.Operations.Where(o => o.Type == Operation.OperationType.Buy).Sum(o => o.TotalPrice);
        ret.TotalSold = asset.Operations.Where(o => o.Type == Operation.OperationType.Sell).Sum(o => o.TotalPrice);
        ret.Credits = BuildCreditInfo(asset.Credits);

        decimal currentVlw = 0;
        foreach (var item in asset.Operations)
        {
            var key = new DateOnly(item.Date.Year, item.Date.Month, 1);
            currentVlw += (item.TotalPrice) * (item.Type == Operation.OperationType.Buy ? 1 : -1);
            ret.InvestedHistory[key] = currentVlw;
        }
        return ret;
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

    private IEnumerable<Operation> GetOperationsByBroker(string brokerName, bool activeOnly) =>
        FilterActiveAssets(GetAssetsByBrokerInternal(brokerName), activeOnly)
            .SelectMany(a => a.Operations);

    private IEnumerable<Credit> GetCreditsByBroker(string brokerName, bool activeOnly) =>
        FilterActiveAssets(GetAssetsByBrokerInternal(brokerName), activeOnly)
            .SelectMany(a => a.Credits);

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
        return GetOperationsByBroker(brokerName, active)
            .Where(o => o.Type == Operation.OperationType.Buy)
            .Sum(o => o.TotalPrice);
    }

    private decimal GetTotalSoldByBroker(string brokerName, bool active)
    {
        return GetOperationsByBroker(brokerName, active)
            .Where(o => o.Type == Operation.OperationType.Sell)
            .Sum(o => o.TotalPrice);
    }

    private CreditInfoDTO GetTotalCreditsByBroker(string brokerName, bool active)
    {
        return BuildCreditInfo(GetCreditsByBroker(brokerName, active));
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
