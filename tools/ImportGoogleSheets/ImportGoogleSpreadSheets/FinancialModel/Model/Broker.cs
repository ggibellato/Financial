using System.Collections.Generic;
namespace FinancialModel.Model
{
    public class Broker
    {
        public string Name { get; }
        public List<Portifolio> Portifolios { get; } = new List<Portifolio>();

        public Broker(string name)
        {
            Name = name;
        }
    }
}