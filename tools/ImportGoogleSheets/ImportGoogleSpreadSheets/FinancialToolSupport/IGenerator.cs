using System.Collections.Generic;

namespace FinancialToolSupport
{
    public interface IGenerator
    {
        void Generate(List<string> fileName);
    }
}
