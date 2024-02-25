using Financial.Application.DTO;
using Financial.Model;
using FinancialModel.Application;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Financial.Infrastructure.Tests")]
namespace FinancialModel.Infrastructure;

public class JSONRepository : IRepository
{

    private Investments _investiments;

    public JSONRepository()
    {
        _investiments = LoadModel();
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
        ret.TotalBoughtActive = GetTotalBoughtByBroker(brokerName, false);
        ret.TotalSoldActive = GetTotalSoldByBroker(brokerName, false);
        ret.TotalCreditsActive = GetTotalCreditsByBroker(brokerName, false);
        ret.PortifiliosActive = GetPortifolioAssetsByBroker(brokerName, true);
        ret.PortifiliosInactive = GetPortifolioAssetsByBroker(brokerName, false);
        return ret;
    }


    public AssetInfoDTO GetAssetInfo(string brokerName, string portifolio, string assetName)
    {
        var ret = new AssetInfoDTO();
        //Asset asset = GetAsset(brokerName, assetName);


        return ret;
    }

    private Asset GetAsset(string brokerName, string portifolioName, string assetName, bool active)
    {
        _investiments.Brokers.Where(b => b.Name == brokerName)
                    .SelectMany(b => b.Portifolios.SelectMany(p => p.Assets.Where(a => a.Active || !active).SelectMany(a => a.Operations)))
                    .Where(o => o.Type == Operation.OperationType.Buy)
                    .Sum(o => o.UnitPrice * o.Quantity + o.Fees);
        return null;
    }

    private decimal GetTotalBoughtByBroker(string brokerName, bool active)
    {
        return _investiments.Brokers.Where(b => b.Name == brokerName)
            .SelectMany(b => b.Portifolios.SelectMany(p => p.Assets.Where(a => a.Active || !active).SelectMany(a => a.Operations)))
            .Where(o => o.Type == Operation.OperationType.Buy)
            .Sum(o => o.UnitPrice * o.Quantity + o.Fees);
    }

    private decimal GetTotalSoldByBroker(string brokerName, bool active)
    {
        return _investiments.Brokers.Where(b => b.Name == brokerName)
            .SelectMany(b => b.Portifolios.SelectMany(p => p.Assets.Where(a => a.Active || !active).SelectMany(a => a.Operations)))
            .Where(o => o.Type == Operation.OperationType.Sell)
            .Sum(o => o.UnitPrice * o.Quantity + o.Fees);
    }

    private decimal GetTotalCreditsByBroker(string brokerName, bool active)
    {
        return _investiments.Brokers.Where(b => b.Name == brokerName)
            .SelectMany(b => b.Portifolios.SelectMany(p => p.Assets.Where(a => a.Active || !active).SelectMany(a => a.Credits)))
            .Sum(o => o.Value);
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


    private Investments LoadModel()
    {
        var modelJson = LoadEmbeddedResource("Data.data.json");
        return Investments.Deserialize(modelJson);
    }

    static string LoadEmbeddedResource(string resourceName)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string fullResourceName = $"{assembly.GetName().Name}.{resourceName}";

        using Stream stream = assembly.GetManifestResourceStream(fullResourceName);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
