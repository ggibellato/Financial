using System.Globalization;
using System.Text.Json.Nodes;
using static Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.DataFileMigration.DataFileMigrationJson;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.DataFileMigration;

public sealed class V4ToV5SettlementAccount : IDataFileMigrationStep
{
    private const string MaeName = "Mãe";
    private const string Brl = "BRL";
    private const string Gbp = "GBP";
    private const string LegacyCollection = "MaeLedgerEntries";

    private static readonly IReadOnlyList<(DateOnly Date, decimal Pounds)> PoundEntries =
    [
        (new DateOnly(2022, 5, 18), 80m),
        (new DateOnly(2022, 11, 25), 100m),
        (new DateOnly(2023, 11, 23), 100m),
        (new DateOnly(2024, 1, 3), 10m),
        (new DateOnly(2024, 10, 11), 80m)
    ];

    public int FromVersion => 4;

    public string Name => nameof(V4ToV5SettlementAccount);

    public string Apply(JsonObject root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var accounts = Objects(root, "Accounts");
        EnsureNoSettlementAccount(accounts);
        EnsureNoAccountNamedMae(accounts);

        var maeId = Guid.NewGuid().ToString();
        var legacyEntries = Objects(root, LegacyCollection);
        var entries = legacyEntries.Select(entry => ToSettlementTransaction(entry, maeId)).ToList();
        var totals = EnsureTotalsUnchanged(legacyEntries, entries);

        var accountArray = root["Accounts"] as JsonArray ?? [];
        accountArray.Add(NewMaeAccount(maeId));
        root["Accounts"] = accountArray;
        root["SettlementTransactions"] = new JsonArray(entries.Select(entry => (JsonNode)entry.Json).ToArray());
        root.Remove(LegacyCollection);

        var paidByYou = entries.Count(entry => entry.PaidByYou);
        return $"{MaeName} Settlement account created with {entries.Count} entries ({paidByYou} paid by you, {entries.Count - paidByYou} by {MaeName}); "
            + $"{entries.Count(entry => entry.IsPoundEntry)} pound entries recorded in {Gbp}; "
            + $"{entries.Count(entry => entry.Json["ConvertedValue"] is null)} without a converted value; "
            + $"totals {Brl} {Format(totals.Brl)} and {Gbp} {Format(totals.Gbp)} unchanged";
    }

    public static (decimal Brl, decimal Gbp) EnsureTotalsUnchanged(IEnumerable<JsonObject> legacyEntries, IReadOnlyList<MigratedEntry> entries)
    {
        var legacy = legacyEntries.ToList();
        var before = (Brl: legacy.Sum(entry => Value(entry, ValueProperty(Brl)) ?? 0m), Gbp: legacy.Sum(entry => Value(entry, ValueProperty(Gbp)) ?? 0m));
        var after = (Brl: entries.Sum(entry => entry.SignedValueIn(Brl)), Gbp: entries.Sum(entry => entry.SignedValueIn(Gbp)));

        if (before != after)
        {
            throw new InvalidDataException(
                $"{MaeName} totals would change: {Brl} {Format(before.Brl)} → {Format(after.Brl)}, {Gbp} {Format(before.Gbp)} → {Format(after.Gbp)}.");
        }

        return before;
    }

    private static MigratedEntry ToSettlementTransaction(JsonObject entry, string maeId)
    {
        var id = IdText(entry["Id"], LegacyCollection);
        var sourceCurrency = entry["SourceCurrency"]?.GetValue<string>();
        if (sourceCurrency is not (Brl or Gbp))
        {
            throw new InvalidDataException($"MaeLedgerEntry {id} has currency {sourceCurrency}; only {Brl} and {Gbp} are allowed.");
        }

        var date = DateOnly.Parse(entry["Date"]!.GetValue<string>(), CultureInfo.InvariantCulture);
        var gbpValue = Value(entry, ValueProperty(Gbp));
        var isPoundEntry = sourceCurrency == Brl && PoundEntries.Contains((date, gbpValue ?? 0m));
        var currency = isPoundEntry ? Gbp : sourceCurrency;
        var otherCurrency = currency == Brl ? Gbp : Brl;

        var paidValue = Value(entry, ValueProperty(currency));
        if (paidValue is null or 0m)
        {
            throw new InvalidDataException($"MaeLedgerEntry {id} has no {currency} value.");
        }

        var otherValue = Value(entry, ValueProperty(otherCurrency));
        if (otherValue is not null && Math.Sign(otherValue.Value) != Math.Sign(paidValue.Value))
        {
            throw new InvalidDataException($"MaeLedgerEntry {id} has BRL and GBP values with different signs.");
        }

        var json = new JsonObject
        {
            ["Id"] = entry["Id"]!.DeepClone(),
            ["AccountId"] = maeId,
            ["Date"] = entry["Date"]!.DeepClone(),
            ["Description"] = entry["Description"]?.DeepClone(),
            ["Note"] = entry["Note"]?.DeepClone() ?? string.Empty,
            ["PaidBy"] = paidValue > 0 ? "You" : "Other",
            ["Currency"] = currency,
            ["Amount"] = Math.Abs(paidValue.Value),
            ["ConvertedValue"] = otherValue is null ? null : Math.Abs(otherValue.Value)
        };
        return new MigratedEntry(json, isPoundEntry);
    }

    private static string ValueProperty(string currency) => currency == Brl ? "BrlValue" : "GbpValue";

    private static decimal? Value(JsonObject entry, string property) =>
        entry[property] is JsonValue value ? value.GetValue<decimal>() : null;

    private static string Format(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    private static void EnsureNoSettlementAccount(IEnumerable<JsonObject> accounts)
    {
        var settlementAccount = accounts.FirstOrDefault(account => account["Template"]?.GetValue<string>() == "Settlement");
        if (settlementAccount is not null)
        {
            throw new InvalidDataException(
                $"'{settlementAccount["Name"]?.GetValue<string>()}' is a Settlement account; Settlement accounts start in V5. Remove it and migrate again.");
        }
    }

    private static void EnsureNoAccountNamedMae(IEnumerable<JsonObject> accounts)
    {
        if (accounts.Any(account => string.Equals(account["Name"]?.GetValue<string>(), MaeName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidDataException($"An account named \"{MaeName}\" already exists.");
        }
    }

    private static JsonObject NewMaeAccount(string id) => new()
    {
        ["Id"] = id,
        ["Name"] = MaeName,
        ["Template"] = "Settlement",
        ["Currency"] = Brl,
        ["IsActive"] = true,
        ["OpeningBalance"] = null,
        ["RoundUp"] = null,
        ["Pots"] = null,
        ["CategoryId"] = null,
        ["Currencies"] = new JsonArray(Brl, Gbp),
        ["OtherParticipant"] = MaeName
    };

    public sealed record MigratedEntry(JsonObject Json, bool IsPoundEntry)
    {
        public bool PaidByYou => Json["PaidBy"]!.GetValue<string>() == "You";

        public decimal SignedValueIn(string currency)
        {
            var value = currency == Json["Currency"]!.GetValue<string>() ? Value(Json, "Amount") : Value(Json, "ConvertedValue");
            return (PaidByYou ? 1m : -1m) * (value ?? 0m);
        }
    }
}
