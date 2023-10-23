using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using Financial.Common;

namespace Financial.Model;

public class Investments
{
    [JsonInclude]
    public List<Broker> Brokers { get; private set; } = new List<Broker>();

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
}
