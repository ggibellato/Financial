using System.Collections.Generic;

namespace Financial.Domain.Entities;

public class Investments
{
    private List<Broker> _brokers = new List<Broker>();
    public IReadOnlyCollection<Broker> Brokers { get => _brokers.AsReadOnly(); set => SetBrokers(value); }
    private void SetBrokers(IReadOnlyCollection<Broker> data)
    {
        _brokers.Clear();
        _brokers.AddRange(data);
    }

    private Investments() {}

    public static Investments Create() => new();

    public void AddBroker(Broker broker)
    {
        _brokers.Add(broker);
    }
}
