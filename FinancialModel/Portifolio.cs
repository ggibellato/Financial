using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Financial.Model;

public class Portifolio
{
    [JsonInclude]
    public string Name { get; private set; }
    [JsonInclude]
    public List<Asset> Assets { get; private set; } = new List<Asset>();

    [JsonConstructor]
    private Portifolio()
    {
    }

    private Portifolio(string name) : this() 
    {
        Name = name;
    }

    internal static Portifolio Create(string name) => new Portifolio(name);
}