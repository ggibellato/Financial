using Financial.Domain.Entities;
using System.Collections.Generic;

namespace Financial.Application.Interfaces;

public interface IDividendDataSource
{
    IReadOnlyList<DividendValue> GetDividends(string ticker);
}
