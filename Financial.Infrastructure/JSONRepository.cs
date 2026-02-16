using Financial.Application.DTO;
using Financial.Model;
using FinancialModel.Application;
using System;
using System.Runtime.CompilerServices;
using System.IO;

[assembly: InternalsVisibleTo("Financial.Infrastructure.Tests")]
namespace FinancialModel.Infrastructure;

public class JSONRepository : IRepository
{

    public const string DataJsonPathConfigurationKey = "DataJsonPath";
    public const string DefaultDataFileName = "data.json";

    private Investments _investiments;
    private readonly string _dataFilePath;

    public JSONRepository() : this(null)
    {
    }

    public JSONRepository(string? dataFilePath)
    {
        _dataFilePath = ResolveDataFilePath(dataFilePath);
        _investiments = LoadModel(_dataFilePath);
    }

    public List<string> GetAllAssetsFullName()
    {
        var result = new List<string>();
        _investiments.Brokers.ToList().ForEach(b =>
        {
            b.Portifolios.ToList().ForEach(p =>
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
        return _investiments.Brokers.Where(b => b.Name == name).SelectMany(b => b.Portifolios.SelectMany(p => p.Assets));
    }

    public IEnumerable<Asset> GetAssetsByBrokerPortifolio(string broker, string protifolio)
    {
        return _investiments.Brokers.Where(b => b.Name == broker)
            .SelectMany(b => b.Portifolios.Where(p => p.Name == protifolio).SelectMany(p => p.Assets));
    }


    public IEnumerable<Asset> GetAssetsByPortifolio(string name)
    {
        return _investiments.Brokers.SelectMany(b => b.Portifolios.Where(p => p.Name == name).SelectMany(p => p.Assets));
    }

    public IEnumerable<Asset> GetAssetsByAssetName(string name)
    {
        return _investiments.Brokers.SelectMany(b => b.Portifolios.SelectMany(p => p.Assets.Where(a => a.Name == name)));
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

        ret.PortifiliosActive = GetPortifolioAssetsByBroker(brokerName, true);
        ret.PortifiliosInactive = GetPortifolioAssetsByBroker(brokerName, false);
        return ret;
    }


    public AssetInfoDTO GetAssetInfo(string brokerName, string portifolio, string assetName)
    {
        var ret = new AssetInfoDTO();
        var asset = _investiments.Brokers
            .First(b => b.Name == brokerName)
            .Portifolios.First(p => p.Name == portifolio)
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
            .SelectMany(b => b.Portifolios.SelectMany(p => p.Assets.Where(a => a.Active || !active).SelectMany(a => a.Operations)))
            .Where(o => o.Type == Operation.OperationType.Buy)
            .Sum(o => o.TotalPrice);
    }

    private decimal GetTotalSoldByBroker(string brokerName, bool active)
    {
        return _investiments.Brokers.Where(b => b.Name == brokerName)
            .SelectMany(b => b.Portifolios.SelectMany(p => p.Assets.Where(a => a.Active || !active).SelectMany(a => a.Operations)))
            .Where(o => o.Type == Operation.OperationType.Sell)
            .Sum(o => o.TotalPrice);
    }

    private  CreditInfoDTO GetTotalCreditsByBroker(string brokerName, bool active)
    {
        var creditsInfo = new CreditInfoDTO();
        var credits = _investiments.Brokers.Where(b => b.Name == brokerName)
            .SelectMany(b => b.Portifolios.SelectMany(p => p.Assets.Where(a => a.Active || !active).SelectMany(a => a.Credits)));
        creditsInfo.Total = credits.Sum(o => o.Value);
        creditsInfo.CreditsByMonth = credits
            .GroupBy(c => new DateOnly(c.Date.Year, c.Date.Month, 1))
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Value));
        return creditsInfo;
    }

    private List<PortifolioDTO> GetPortifolioAssetsByBroker(string brokerName, bool active)
    {
        var ret = new List<PortifolioDTO>();
        var broker = _investiments.Brokers.Where(b => b.Name == brokerName).First();
        foreach(var p in broker.Portifolios)
        {
            var assets = p.Assets.Where(a => a.Active == active);
            if(assets.Any())
            {
                var pDTO = new PortifolioDTO
                {
                    Name = p.Name, 
                    Assets = assets.Select(a => a.Name).ToList()
                };
                ret.Add(pDTO);
            }
        }
        return ret;
    }


    private static string ResolveDataFilePath(string? dataFilePath)
    {
        var resolvedPath = string.IsNullOrWhiteSpace(dataFilePath)
            ? Path.Combine(AppContext.BaseDirectory, DefaultDataFileName)
            : dataFilePath;

        if (Directory.Exists(resolvedPath))
        {
            resolvedPath = Path.Combine(resolvedPath, DefaultDataFileName);
        }

        if (!Path.IsPathRooted(resolvedPath))
        {
            resolvedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, resolvedPath));
        }

        return resolvedPath;
    }

    private static Investments LoadModel(string dataFilePath)
    {
        if (!File.Exists(dataFilePath))
        {
            throw new FileNotFoundException(
                $"Data file not found at '{dataFilePath}'. Configure '{DataJsonPathConfigurationKey}' or place '{DefaultDataFileName}' in the application directory.",
                dataFilePath);
        }

        var modelJson = File.ReadAllText(dataFilePath);
        return Investments.Deserialize(modelJson);
    }
}
