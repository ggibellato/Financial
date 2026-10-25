using System.Text.Json.Nodes;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.DataFileMigration;

public sealed class V2ToV3CardsToAccounts : IDataFileMigrationStep
{
    private static readonly (string Collection, string Record)[] CardReferences =
    [
        ("Expenses", "Expense"),
        ("CardStatements", "CardStatement"),
        ("InvestmentAccounts", "InvestmentAccount")
    ];

    public int FromVersion => 2;

    public string Name => nameof(V2ToV3CardsToAccounts);

    public string Apply(JsonObject root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var cards = Objects(root, "CreditCards");
        var cardIds = cards.Select(card => IdText(card["Id"], "CreditCard")).ToList();
        foreach (var reference in CardReferences)
        {
            EnsureCardReferencesExist(root, reference, cardIds.ToHashSet(StringComparer.OrdinalIgnoreCase));
        }

        var accounts = root["Accounts"] as JsonArray ?? [];
        foreach (var card in cards)
        {
            accounts.Add(ToAccount(card));
        }

        root["Accounts"] = accounts;

        var statements = root["CardStatements"] as JsonArray ?? [];
        root["CardStatements"] = statements;
        foreach (var statement in statements.OfType<JsonObject>())
        {
            statement["DueDate"] = null;
        }

        var charges = Objects(root, "Expenses")
            .Where(expense => expense["CreditCardId"] is not null && expense["InvoiceDate"] is not null)
            .Select(expense => new Charge(
                IdText(expense["CreditCardId"], $"Expense {expense["Id"]} CreditCardId"),
                DateOnly.Parse(expense["InvoiceDate"]!.GetValue<string>()),
                expense["PaymentSourceAccountId"] is not null))
            .ToList();

        var added = 0;
        foreach (var cardId in cardIds)
        {
            var months = charges
                .Where(charge => SameId(charge.CardId, cardId))
                .Select(charge => (charge.InvoiceDate.Year, charge.InvoiceDate.Month))
                .Distinct()
                .OrderBy(month => month);

            foreach (var (year, month) in months)
            {
                if (StatementFor(statements, cardId, year, month) is null)
                {
                    statements.Add(NewStatement(cardId, year, month, charges));
                    added++;
                }
            }
        }

        var moved = 0;
        foreach (var card in cards)
        {
            if (card["NextInvoiceDueDate"] is not { } dueDateNode)
            {
                continue;
            }

            var cardId = IdText(card["Id"], "CreditCard");
            var dueDate = DateOnly.Parse(dueDateNode.GetValue<string>());
            var statement = StatementFor(statements, cardId, dueDate.Year, dueDate.Month);
            if (statement is null)
            {
                statement = NewStatement(cardId, dueDate.Year, dueDate.Month, charges);
                statements.Add(statement);
                added++;
            }

            statement["DueDate"] = dueDateNode.DeepClone();
            moved++;
        }

        root.Remove("CreditCards");

        return $"{cards.Count} cards became accounts; {added} statements added; {moved} due dates moved to statements";
    }

    private static JsonObject ToAccount(JsonObject card) => new()
    {
        ["Id"] = card["Id"]?.DeepClone(),
        ["Name"] = card["Name"]?.DeepClone(),
        ["Template"] = "CreditCard",
        ["Currency"] = "GBP",
        ["IsActive"] = card["IsActive"]?.DeepClone() ?? true,
        ["OpeningBalance"] = null,
        ["RoundUp"] = null
    };

    private static JsonObject NewStatement(string cardId, int year, int month, IReadOnlyList<Charge> charges) => new()
    {
        ["Id"] = Guid.NewGuid().ToString(),
        ["CreditCardId"] = cardId,
        ["Year"] = year,
        ["Month"] = month,
        ["IsPaid"] = charges
            .Where(charge => SameId(charge.CardId, cardId) && charge.InvoiceDate.Year == year && charge.InvoiceDate.Month == month)
            .Select(charge => charge.IsSettled)
            .DefaultIfEmpty(false)
            .All(isSettled => isSettled),
        ["DueDate"] = null
    };

    private static JsonObject? StatementFor(JsonArray statements, string cardId, int year, int month) =>
        statements.OfType<JsonObject>().FirstOrDefault(statement =>
            SameId(IdText(statement["CreditCardId"], $"CardStatement {statement["Id"]} CreditCardId"), cardId)
            && statement["Year"]?.GetValue<int>() == year
            && statement["Month"]?.GetValue<int>() == month);

    private static void EnsureCardReferencesExist(JsonObject root, (string Collection, string Record) reference, IReadOnlySet<string> cardIds)
    {
        foreach (var record in Objects(root, reference.Collection))
        {
            if (record["CreditCardId"] is not { } value)
            {
                continue;
            }

            var cardId = IdText(value, $"{reference.Record} {record["Id"]} CreditCardId");
            if (!cardIds.Contains(cardId))
            {
                throw new InvalidDataException($"{reference.Record} {record["Id"]} references unknown card {cardId}");
            }
        }
    }

    private static List<JsonObject> Objects(JsonObject root, string collection) =>
        (root[collection] as JsonArray ?? [])
            .Select(item => item as JsonObject ?? throw new InvalidDataException($"{collection} contains a value that is not an object: {item?.ToJsonString() ?? "null"}"))
            .ToList();

    private static bool SameId(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static string IdText(JsonNode? id, string owner) =>
        id is JsonValue value && value.TryGetValue<string>(out var text)
            ? text
            : throw new InvalidDataException($"{owner} has an Id that is not a string: {id?.ToJsonString() ?? "null"}");

    private sealed record Charge(string CardId, DateOnly InvoiceDate, bool IsSettled);
}
