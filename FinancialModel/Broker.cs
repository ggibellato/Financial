using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Financial.Model;

public class Broker
{
    [JsonInclude]
    public string Name { get; private set; }
    [JsonInclude]
    public string Currency { get; private set; }
    [JsonInclude]
    public List<Portifolio> Portifolios { get; private set; } = new List<Portifolio>();

    [JsonConstructor]
    private Broker() {}

    private Broker(string name, string currency) : this()
    {
        Name = name;
        Currency = currency;
    }

    public static Broker Create(string name, string currency) => new(name, currency);

    public Portifolio AddPortifolio(string name)
    {
        var portifolio = Portifolios.FirstOrDefault(p => p.Name == name);
        if (portifolio is null)
        {
            portifolio = Portifolio.Create(name);
            Portifolios.Add(portifolio);
        }
        return portifolio;
    }
}