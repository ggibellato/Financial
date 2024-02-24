using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using Financial.Common;
using System;

namespace Financial.Model;

public class Investments
{
    private List<Broker> _brokers = new List<Broker>();
    [JsonInclude]
    public IReadOnlyCollection<Broker> Brokers { get => _brokers.AsReadOnly(); set => SetBrokers(value); }
    private void SetBrokers(IReadOnlyCollection<Broker> data)
    {
        _brokers.Clear();
        _brokers.AddRange(data);
    }

    [JsonConstructor]
    private Investments() {}

    public static Investments Create() => new();

    public string Serialize()
    {
        var options = new JsonSerializerOptions()
        {
            Converters = { new JsonStringEnumConverter() },
            WriteIndented = true,
            TypeInfoResolver = new PrivateConstructorContractResolver()
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return JsonSerializer.Serialize(this, options);
    }

    public static Investments Deserialize(string json)
    {
        var options = new JsonSerializerOptions()
        {
            Converters = { new JsonStringEnumConverter() },
            WriteIndented = true,
            TypeInfoResolver = new PrivateConstructorContractResolver()
        };
        var investments = JsonSerializer.Deserialize<Investments>(json, options);
        return investments;
    }

    public void AddBroker(Broker broker)
    {
        _brokers.Add(broker);
    }
}
