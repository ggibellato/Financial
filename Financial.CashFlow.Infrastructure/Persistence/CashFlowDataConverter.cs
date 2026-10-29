using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Financial.CashFlow.Infrastructure.Persistence;

public sealed class CashFlowDataConverter : JsonConverter<CashFlowData>
{
    private static readonly JsonSerializerOptions UnresolvedOptions = CashFlowElementOptions.Create(context: null);

    public override CashFlowData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var unresolvedOptions = UnresolvedOptions;
        var context = new ReferenceResolutionContext();
        var resolvedOptions = CashFlowElementOptions.Create(context);

        var categories = DeserializeCollection<Category>(root, "Categories", unresolvedOptions);
        foreach (var category in categories) context.Categories[category.Id] = category;

        var accounts = DeserializeCollection<Account>(root, "Accounts", resolvedOptions);
        foreach (var account in accounts) context.Accounts[account.Id] = account;
        var incomeSources = DeserializeCollection<IncomeSource>(root, "IncomeSources", resolvedOptions);
        foreach (var incomeSource in incomeSources) context.IncomeSources[incomeSource.Id] = incomeSource;
        foreach (var pot in accounts.SelectMany(account => account.Pots ?? [])) context.ReserveBuckets[pot.Id] = pot;

        var investmentAccounts = DeserializeCollection<InvestmentAccount>(root, "InvestmentAccounts", resolvedOptions);
        foreach (var investmentAccount in investmentAccounts) context.InvestmentAccounts[investmentAccount.Id] = investmentAccount;

        var data = CashFlowData.Create();
        foreach (var account in accounts) data.AddAccount(account);
        foreach (var incomeSource in incomeSources) data.AddIncomeSource(incomeSource);
        foreach (var investmentAccount in investmentAccounts) data.AddInvestmentAccount(investmentAccount);
        foreach (var category in categories) data.AddCategory(category);

        foreach (var expense in DeserializeCollection<Expense>(root, "Expenses", resolvedOptions)) data.AddExpense(expense);

        // Incomes must resolve - and populate context.Incomes - before ReserveMovements, since a
        // linked movement's IncomeId reference converter reads that same lookup at Read time.
        var incomes = DeserializeCollection<Income>(root, "Incomes", resolvedOptions);
        foreach (var income in incomes) context.Incomes[income.Id] = income;
        foreach (var income in incomes) data.AddIncome(income);

        foreach (var movement in DeserializeCollection<ReserveMovement>(root, "ReserveMovements", resolvedOptions)) data.AddReserveMovement(movement);
        foreach (var statement in DeserializeCollection<CardStatement>(root, "CardStatements", resolvedOptions)) data.AddCardStatement(statement);
        foreach (var bill in DeserializeCollection<RecurringBill>(root, "RecurringBills", resolvedOptions)) data.AddRecurringBill(bill);
        foreach (var entry in DeserializeCollection<MaeLedgerEntry>(root, "MaeLedgerEntries", resolvedOptions)) data.AddMaeLedgerEntry(entry);
        foreach (var snapshot in DeserializeCollection<InvestmentSnapshot>(root, "InvestmentSnapshots", resolvedOptions)) data.AddInvestmentSnapshot(snapshot);
        foreach (var transfer in DeserializeCollection<Transfer>(root, "Transfers", resolvedOptions)) data.AddTransfer(transfer);
        foreach (var adjustment in DeserializeCollection<BalanceAdjustment>(root, "BalanceAdjustments", resolvedOptions)) data.AddBalanceAdjustment(adjustment);
        foreach (var decision in DeserializeCollection<TitheCarryForward>(root, "TitheCarryForwards", resolvedOptions)) data.AddTitheCarryForward(decision);

        if (root.TryGetProperty("TitheCarryForwardEffectiveFrom", out var effectiveFromElement)
            && effectiveFromElement.ValueKind != JsonValueKind.Null)
        {
            data.SetTitheCarryForwardEffectiveFrom(effectiveFromElement.Deserialize<DateOnly>(unresolvedOptions));
        }

        return data;
    }

    public override void Write(Utf8JsonWriter writer, CashFlowData value, JsonSerializerOptions options)
    {
        var elementOptions = UnresolvedOptions;

        writer.WriteStartObject();
        WriteCollection(writer, "Expenses", value.Expenses, elementOptions);
        WriteCollection(writer, "ReserveMovements", value.ReserveMovements, elementOptions);
        WriteCollection(writer, "CardStatements", value.CardStatements, elementOptions);
        WriteCollection(writer, "RecurringBills", value.RecurringBills, elementOptions);
        WriteCollection(writer, "MaeLedgerEntries", value.MaeLedgerEntries, elementOptions);
        WriteCollection(writer, "InvestmentSnapshots", value.InvestmentSnapshots, elementOptions);
        WriteCollection(writer, "InvestmentAccounts", value.InvestmentAccounts, elementOptions);
        WriteCollection(writer, "Accounts", value.Accounts, elementOptions);
        WriteCollection(writer, "IncomeSources", value.IncomeSources, elementOptions);
        WriteCollection(writer, "Incomes", value.Incomes, elementOptions);
        WriteCollection(writer, "Categories", value.Categories, elementOptions);
        WriteCollection(writer, "Transfers", value.Transfers, elementOptions);
        WriteCollection(writer, "BalanceAdjustments", value.BalanceAdjustments, elementOptions);
        WriteCollection(writer, "TitheCarryForwards", value.TitheCarryForwards, elementOptions);

        writer.WritePropertyName("TitheCarryForwardEffectiveFrom");
        if (value.TitheCarryForwardEffectiveFrom.HasValue)
        {
            JsonSerializer.Serialize(writer, value.TitheCarryForwardEffectiveFrom.Value, elementOptions);
        }
        else
        {
            writer.WriteNullValue();
        }

        writer.WriteEndObject();
    }

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
