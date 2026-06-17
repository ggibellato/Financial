#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financial.Infrastructure.Integrations.FinancialToolSupport;

public interface IGenerator
{
    Task GenerateAsync(List<string> fileNames, IProgress<string>? progress = null);
}

