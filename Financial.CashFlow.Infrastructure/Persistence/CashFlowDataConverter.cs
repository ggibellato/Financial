using Financial.CashFlow.Domain.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Financial.CashFlow.Infrastructure.Persistence;

/// <summary>
/// Top-level (de)serializer for <see cref="CashFlowData"/>. On read, resolves the seeded
/// Bank/IncomeSource/InvestmentAccount collections first - regardless of their position in the
/// JSON text - then deserializes every other collection through reference converters bound to
/// that resolution, assembling the result via <see cref="CashFlowData.Create"/>/<c>Add*</c> so
/// every reference property shares the exact same instance as its owning collection entry.
/// </summary>
public sealed class CashFlowDataConverter : JsonConverter<CashFlowData>
{
    public override CashFlowData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var unresolvedOptions = CreateElementOptions(context: null);
        var banks = DeserializeCollection<Bank>(root, "Banks", unresolvedOptions);
        var incomeSources = DeserializeCollection<IncomeSource>(root, "IncomeSources", unresolvedOptions);
        var investmentAccounts = DeserializeCollection<InvestmentAccount>(root, "InvestmentAccounts", unresolvedOptions);
        var reserveBuckets = DeserializeCollection<ReserveBucket>(root, "ReserveBuckets", unresolvedOptions);

        var context = new ReferenceResolutionContext();
        foreach (var bank in banks) context.Banks[bank.Id] = bank;
        foreach (var incomeSource in incomeSources) context.IncomeSources[incomeSource.Id] = incomeSource;
        foreach (var account in investmentAccounts) context.InvestmentAccounts[account.Id] = account;
        foreach (var bucket in reserveBuckets) context.ReserveBuckets[bucket.Id] = bucket;

        var resolvedOptions = CreateElementOptions(context);

        var data = CashFlowData.Create();
        foreach (var bank in banks) data.AddBank(bank);
        foreach (var incomeSource in incomeSources) data.AddIncomeSource(incomeSource);
        foreach (var account in investmentAccounts) data.AddInvestmentAccount(account);
        foreach (var bucket in reserveBuckets) data.AddReserveBucket(bucket);

        foreach (var expense in DeserializeCollection<Expense>(root, "Expenses", resolvedOptions)) data.AddExpense(expense);
        foreach (var movement in DeserializeCollection<ReserveMovement>(root, "ReserveMovements", resolvedOptions)) data.AddReserveMovement(movement);
        foreach (var statement in DeserializeCollection<CardStatement>(root, "CardStatements", resolvedOptions)) data.AddCardStatement(statement);
        foreach (var bill in DeserializeCollection<RecurringBill>(root, "RecurringBills", resolvedOptions)) data.AddRecurringBill(bill);
        foreach (var entry in DeserializeCollection<MaeLedgerEntry>(root, "MaeLedgerEntries", resolvedOptions)) data.AddMaeLedgerEntry(entry);
        foreach (var snapshot in DeserializeCollection<InvestmentSnapshot>(root, "InvestmentSnapshots", resolvedOptions)) data.AddInvestmentSnapshot(snapshot);
        foreach (var income in DeserializeCollection<Income>(root, "Incomes", resolvedOptions)) data.AddIncome(income);
        foreach (var transfer in DeserializeCollection<Transfer>(root, "Transfers", resolvedOptions)) data.AddTransfer(transfer);
        foreach (var adjustment in DeserializeCollection<BalanceAdjustment>(root, "BalanceAdjustments", resolvedOptions)) data.AddBalanceAdjustment(adjustment);

        return data;
    }

    public override void Write(Utf8JsonWriter writer, CashFlowData value, JsonSerializerOptions options)
    {
        var elementOptions = CreateElementOptions(context: null);

        writer.WriteStartObject();
        WriteCollection(writer, "ReserveBuckets", value.ReserveBuckets, elementOptions);
        WriteCollection(writer, "Expenses", value.Expenses, elementOptions);
        WriteCollection(writer, "ReserveMovements", value.ReserveMovements, elementOptions);
        WriteCollection(writer, "CardStatements", value.CardStatements, elementOptions);
        WriteCollection(writer, "RecurringBills", value.RecurringBills, elementOptions);
        WriteCollection(writer, "MaeLedgerEntries", value.MaeLedgerEntries, elementOptions);
        WriteCollection(writer, "InvestmentSnapshots", value.InvestmentSnapshots, elementOptions);
        WriteCollection(writer, "InvestmentAccounts", value.InvestmentAccounts, elementOptions);
        WriteCollection(writer, "Banks", value.Banks, elementOptions);
        WriteCollection(writer, "IncomeSources", value.IncomeSources, elementOptions);
        WriteCollection(writer, "Incomes", value.Incomes, elementOptions);
        WriteCollection(writer, "Transfers", value.Transfers, elementOptions);
        WriteCollection(writer, "BalanceAdjustments", value.BalanceAdjustments, elementOptions);
        writer.WriteEndObject();
    }

    private static JsonSerializerOptions CreateElementOptions(ReferenceResolutionContext? context) => new()
    {
        Converters = { new JsonStringEnumConverter() },
        TypeInfoResolver = new CashFlowTypeInfoResolver(context)
    };

    private static List<T> DeserializeCollection<T>(JsonElement root, string propertyName, JsonSerializerOptions options)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return [];
        }

        try
        {
            return element.Deserialize<List<T>>(options) ?? [];
        }
        catch (JsonException ex) when (ex.Message.Contains("required", StringComparison.OrdinalIgnoreCase))
        {
            throw new JsonException(
                $"'{propertyName}' contains a record still in the pre-migration string shape (missing a *Id reference field). Run the appropriate reference migration before loading this file.",
                ex);
        }
    }

    private static void WriteCollection<T>(Utf8JsonWriter writer, string propertyName, IEnumerable<T> collection, JsonSerializerOptions options)
    {
        writer.WritePropertyName(propertyName);
        JsonSerializer.Serialize(writer, collection, options);
    }
}
