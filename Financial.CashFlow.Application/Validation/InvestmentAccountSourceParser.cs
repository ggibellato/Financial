using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Validation;

namespace Financial.CashFlow.Application.Validation;

public static class InvestmentAccountSourceParser
{
    private const string LegacyPotsAccountTotalWireName = "ReserveBucketsSum";

    public static bool TryParse(string? value, out InvestmentAccountSource source)
    {
        if (string.Equals(value, LegacyPotsAccountTotalWireName, StringComparison.OrdinalIgnoreCase))
        {
            source = InvestmentAccountSource.PotsAccountTotal;
            return true;
        }

        return EnumParser.TryParseEnum(value, out source) && source != InvestmentAccountSource.PotsAccountTotal;
    }

    public static string WireName(InvestmentAccountSource source) =>
        source == InvestmentAccountSource.PotsAccountTotal ? LegacyPotsAccountTotalWireName : source.ToString();
}
