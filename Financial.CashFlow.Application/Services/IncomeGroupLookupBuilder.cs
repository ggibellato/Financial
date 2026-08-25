using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.Services;

internal static class IncomeGroupLookupBuilder
{
    internal static Dictionary<string, IncomeGroup> Build(IEnumerable<IncomeSource> incomeSources) =>
        incomeSources.ToDictionary(s => s.Name, s => s.Group, StringComparer.OrdinalIgnoreCase);
}
