using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinancialToolSupport;

public interface IGenerator
{
    Task GenerateAsync(List<string> fileName, IProgress<string> progress = null);
}
