using System.Collections.Generic;

namespace Financial.Investment.Domain.Entities;

public class Investments
{
    private List<Broker> _activeBrokers = new List<Broker>();
    public IReadOnlyCollection<Broker> ActiveBrokers { get => _activeBrokers.AsReadOnly(); private set => SetActiveBrokers(value); }
    private void SetActiveBrokers(IReadOnlyCollection<Broker> data)
    {
        _activeBrokers.Clear();
        _activeBrokers.AddRange(data);
    }

    private List<Broker> _historicBrokers = new List<Broker>();
    public IReadOnlyCollection<Broker> HistoricBrokers { get => _historicBrokers.AsReadOnly(); private set => SetHistoricBrokers(value); }
    private void SetHistoricBrokers(IReadOnlyCollection<Broker> data)
    {
        _historicBrokers.Clear();
        _historicBrokers.AddRange(data);
    }

    private Investments() { }

    public static Investments Create() => new();

    public void AddActiveBroker(Broker broker)
    {
        _activeBrokers.Add(broker);
    }

    public void AddHistoricBroker(Broker broker)
    {
        _historicBrokers.Add(broker);
    }
}
