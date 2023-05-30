using System.Collections.Generic;

namespace FinancialModel.Model
{
    public class Portifolio
    {
        public string Name { get; }
        public List<Asset> Assets { get; } = new List<Asset>();

        public Portifolio(string name)
        {
            Name = name;
        }
    }
}