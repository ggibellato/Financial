using System.Collections.Generic;
using System.Linq;

namespace Financial.Domain.Entities;

public class Broker
{
    public string Name { get; private set; }
    public string Currency { get; private set; }

    private List<Portfolio> _portfolios = new List<Portfolio>();
    public IReadOnlyCollection<Portfolio> Portfolios { get => _portfolios.AsReadOnly(); set => SetPortfolios(value); }
    private void SetPortfolios(IReadOnlyCollection<Portfolio> data)
    {
        _portfolios.Clear();
        _portfolios.AddRange(data);
    }

    private Broker() {}

    private Broker(string name, string currency) : this()
    {
        Name = name;
        Currency = currency;
    }

    public static Broker Create(string name, string currency) => new(name, currency);

    public Portfolio AddPortfolio(string name)
    {
        var portfolio = Portfolios.FirstOrDefault(p => p.Name == name);
        if (portfolio is null)
        {
            portfolio = Portfolio.Create(name);
            _portfolios.Add(portfolio);
        }
        return portfolio;
    }
}
