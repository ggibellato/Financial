using System.Collections.Generic;

namespace FinancialModel.Model
{
    public class Asset
    {
        public string Name { get; set; }

        public List<Operation> Operations { get; set; } = new List<Operation>();

        public List<Credit> Credits { get; set; } = new List<Credit>();

        public Asset(string name)
        {
            Name = name;
        }
    }
}
