using Financial.Application.DTO;
using Financial.Model;
using FinancialModel.Application;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FinancialModel.Infrastructure;

public abstract class InvestmentsRepositoryBase : IRepository
{
    protected Investments _investiments;

    protected InvestmentsRepositoryBase(Investments investiments)
    {
        _investiments = investiments ?? throw new ArgumentNullException(nameof(investiments));
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
        return _investiments.Brokers.Where(b => b.Name == name).SelectMany(b => b.Portfolios.SelectMany(p => p.Assets));
    }

    public IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio)
    {
        return _investiments.Brokers.Where(b => b.Name == broker)
            .SelectMany(b => b.Portfolios.Where(p => p.Name == portfolio).SelectMany(p => p.Assets));
    }

    public IEnumerable<Asset> GetAssetsByPortfolio(string name)
    {
        return _investiments.Brokers.SelectMany(b => b.Portfolios.Where(p => p.Name == name).SelectMany(p => p.Assets));
    }

    public IEnumerable<Asset> GetAssetsByAssetName(string name)
    {
        return _investiments.Brokers.SelectMany(b => b.Portfolios.SelectMany(p => p.Assets.Where(a => a.Name == name)));
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
        var asset = _investiments.Brokers
            .First(b => b.Name == brokerName)
            .Portfolios.First(p => p.Name == portfolio)
            .Assets.First(a => a.Name == assetName);

        ret.Exchange = asset.Exchange;
        ret.Ticker = asset.Ticker;
        ret.Quantity = asset.Quantity;
        ret.AvaragePrice = asset.AvargePrice;
        ret.TotalBought = asset.Operations.Where(o => o.Type == Operation.OperationType.Buy).Sum(o => o.TotalPrice);
        ret.TotalSold = asset.Operations.Where(o => o.Type == Operation.OperationType.Sell).Sum(o => o.TotalPrice);
        var creditsInfo = new CreditInfoDTO();
        creditsInfo.Total = asset.Credits.Sum(o => o.Value);
        creditsInfo.CreditsByMonth = asset.Credits.GroupBy(c => new DateOnly(c.Date.Year, c.Date.Month, 1))
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Value));
        ret.Credits = creditsInfo;

        decimal currentVlw = 0;
        foreach (var item in asset.Operations)
        {
            var key = new DateOnly(item.Date.Year, item.Date.Month, 1);
            currentVlw += (item.TotalPrice) * (item.Type == Operation.OperationType.Buy ? 1 : -1);
            ret.InvestedHistory[key] = currentVlw;
        }
        return ret;
    }

    private decimal GetTotalBoughtByBroker(string brokerName, bool active)
    {
        return _investiments.Brokers.Where(b => b.Name == brokerName)
            .SelectMany(b => b.Portfolios.SelectMany(p => p.Assets.Where(a => a.Active || !active).SelectMany(a => a.Operations)))
            .Where(o => o.Type == Operation.OperationType.Buy)
            .Sum(o => o.TotalPrice);
    }

    private decimal GetTotalSoldByBroker(string brokerName, bool active)
    {
        return _investiments.Brokers.Where(b => b.Name == brokerName)
            .SelectMany(b => b.Portfolios.SelectMany(p => p.Assets.Where(a => a.Active || !active).SelectMany(a => a.Operations)))
            .Where(o => o.Type == Operation.OperationType.Sell)
            .Sum(o => o.TotalPrice);
    }

    private CreditInfoDTO GetTotalCreditsByBroker(string brokerName, bool active)
    {
        var creditsInfo = new CreditInfoDTO();
        var credits = _investiments.Brokers.Where(b => b.Name == brokerName)
            .SelectMany(b => b.Portfolios.SelectMany(p => p.Assets.Where(a => a.Active || !active).SelectMany(a => a.Credits)));
        creditsInfo.Total = credits.Sum(o => o.Value);
        creditsInfo.CreditsByMonth = credits
            .GroupBy(c => new DateOnly(c.Date.Year, c.Date.Month, 1))
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Value));
        return creditsInfo;
    }

    private List<PortfolioDTO> GetPortfolioAssetsByBroker(string brokerName, bool active)
    {
        var ret = new List<PortfolioDTO>();
        var broker = _investiments.Brokers.Where(b => b.Name == brokerName).First();
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
