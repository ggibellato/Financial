using System;
using System.Collections.Generic;

namespace Financial.Domain.Entities;

public enum CountryCode
{
    Unknown = 0,
    BR,
    US,
    UK
}

public enum GlobalAssetClass
{
    Unknown = 0,
    Equity,
    RealEstate,
    Bond,
    Fund,
    ETF,
    Cash,
    Pension,
    Other
}

public static class GlobalAssetClassMapping
{
    private static readonly IReadOnlyDictionary<(CountryCode Country, string LocalTypeCode), GlobalAssetClass> Mapping =
        new Dictionary<(CountryCode, string), GlobalAssetClass>(new CountryLocalTypeComparer())
        {
            [(CountryCode.BR, "Acoes")] = GlobalAssetClass.Equity,
            [(CountryCode.BR, "FII")] = GlobalAssetClass.RealEstate,
            [(CountryCode.BR, "ETF")] = GlobalAssetClass.ETF,
            [(CountryCode.BR, "Fund")] = GlobalAssetClass.Fund,
            [(CountryCode.BR, "Bond")] = GlobalAssetClass.Bond,
            [(CountryCode.US, "REIT")] = GlobalAssetClass.RealEstate,
            [(CountryCode.UK, "REIT")] = GlobalAssetClass.RealEstate,
            [(CountryCode.BR, "TesouroDireto")] = GlobalAssetClass.Bond,
            [(CountryCode.US, "T-Bill")] = GlobalAssetClass.Bond,
            [(CountryCode.UK, "ConventionalGilt")] = GlobalAssetClass.Bond,
            [(CountryCode.US, "Stock")] = GlobalAssetClass.Equity,
            [(CountryCode.UK, "Stock")] = GlobalAssetClass.Equity,
            [(CountryCode.US, "ETF")] = GlobalAssetClass.ETF,
            [(CountryCode.UK, "ETF")] = GlobalAssetClass.ETF,
            [(CountryCode.US, "Fund")] = GlobalAssetClass.Fund,
            [(CountryCode.UK, "Fund")] = GlobalAssetClass.Fund,
            [(CountryCode.US, "Bond")] = GlobalAssetClass.Bond,
            [(CountryCode.UK, "Bond")] = GlobalAssetClass.Bond,
            [(CountryCode.US, "Cash")] = GlobalAssetClass.Cash,
            [(CountryCode.UK, "Cash")] = GlobalAssetClass.Cash,
            [(CountryCode.US, "Pension")] = GlobalAssetClass.Pension,
            [(CountryCode.UK, "Pension")] = GlobalAssetClass.Pension
        };

    public static GlobalAssetClass Resolve(CountryCode country, string localTypeCode)
    {
        if (string.IsNullOrWhiteSpace(localTypeCode))
        {
            return GlobalAssetClass.Unknown;
        }

        var key = (country, localTypeCode.Trim());
        return Mapping.TryGetValue(key, out var assetClass)
            ? assetClass
            : GlobalAssetClass.Unknown;
    }

    private sealed class CountryLocalTypeComparer : IEqualityComparer<(CountryCode Country, string LocalTypeCode)>
    {
        public bool Equals((CountryCode Country, string LocalTypeCode) x, (CountryCode Country, string LocalTypeCode) y)
        {
            return x.Country == y.Country &&
                   string.Equals(x.LocalTypeCode, y.LocalTypeCode, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((CountryCode Country, string LocalTypeCode) obj)
        {
            var hash = new HashCode();
            hash.Add(obj.Country);
            hash.Add(obj.LocalTypeCode, StringComparer.OrdinalIgnoreCase);
            return hash.ToHashCode();
        }
    }
}
