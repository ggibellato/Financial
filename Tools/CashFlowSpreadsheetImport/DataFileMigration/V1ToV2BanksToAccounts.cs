using System.Text.Json.Nodes;
using static Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.DataFileMigration.DataFileMigrationJson;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.DataFileMigration;

public sealed class V1ToV2BanksToAccounts : IDataFileMigrationStep
{
    private const string DefaultStartDate = "0001-01-01";

    private static readonly (string Collection, string Record, string V1Key, string V2Key)[] References =
    [
        ("Expenses", "Expense", "PaymentSourceBankId", "PaymentSourceAccountId"),
        ("Incomes", "Income", "BankId", "AccountId"),
        ("Transfers", "Transfer", "SourceBankId", "SourceAccountId"),
        ("Transfers", "Transfer", "DestinationBankId", "DestinationAccountId"),
        ("BalanceAdjustments", "BalanceAdjustment", "BankId", "AccountId")
    ];

    public int FromVersion => 1;

    public string Name => nameof(V1ToV2BanksToAccounts);

    public string Apply(JsonObject root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var banks = Objects(root, "Banks");
        var bankIds = banks.Select(bank => IdText(bank["Id"], "Bank")).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var accounts = new JsonArray([.. banks.Select(bank => (JsonNode?)ToAccount(bank))]);
        root.Remove("Banks");
        root["Accounts"] = accounts;

        var renamed = References.Sum(reference => RenameReferences(root, reference, bankIds));

        return $"{accounts.Count} banks became accounts; {renamed} references renamed";
    }

    private static JsonObject ToAccount(JsonObject bank) => new()
    {
        ["Id"] = bank["Id"]?.DeepClone(),
        ["Name"] = bank["Name"]?.DeepClone(),
        ["Template"] = "Bank",
        ["Currency"] = "GBP",
        ["IsActive"] = true,
        ["OpeningBalance"] = new JsonObject
        {
            ["Amount"] = bank["OpeningBalance"]?.DeepClone() ?? 0,
            ["StartDate"] = bank["OpeningBalanceDate"]?.DeepClone() ?? DefaultStartDate
        },
        ["RoundUp"] = new JsonObject
        {
            ["Enabled"] = bank["RoundUpEnabled"]?.DeepClone() ?? false
        }
    };

    private static int RenameReferences(
        JsonObject root,
        (string Collection, string Record, string V1Key, string V2Key) reference,
        IReadOnlySet<string> bankIds)
    {
        if (root[reference.Collection] is not JsonArray records)
        {
            return 0;
        }

        var renamed = 0;
        foreach (var record in records.OfType<JsonObject>())
        {
            if (!record.TryGetPropertyValue(reference.V1Key, out var value))
            {
                continue;
            }

            record.Remove(reference.V1Key);
            record[reference.V2Key] = value;

            if (value is null)
            {
                continue;
            }

            var bankId = IdText(value, $"{reference.Record} {record["Id"]} {reference.V1Key}");
            if (!bankIds.Contains(bankId))
            {
                throw new InvalidDataException($"{reference.Record} {record["Id"]} references unknown bank {bankId}");
            }

            renamed++;
        }

        return renamed;
    }
}
