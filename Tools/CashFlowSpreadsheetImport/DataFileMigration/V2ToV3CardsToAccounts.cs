using System.Text.Json.Nodes;
using static Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.DataFileMigration.DataFileMigrationJson;

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
        var knownCardIds = cardIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var reference in CardReferences)
        {
            EnsureCardReferencesExist(root, reference, knownCardIds);
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

        var statementCountBefore = statements.Count;

        var charges = Objects(root, "Expenses")
            .Where(expense => expense["CreditCardId"] is not null && expense["InvoiceDate"] is not null)
            .Select(expense => new Charge(
                IdText(expense["CreditCardId"], $"Expense {expense["Id"]} CreditCardId"),
                DateOnly.Parse(expense["InvoiceDate"]!.GetValue<string>()),
                expense["PaymentSourceAccountId"] is not null))
            .ToList();

        foreach (var cardId in cardIds)
        {
            var months = charges
                .Where(charge => SameId(charge.CardId, cardId))
                .Select(charge => (charge.InvoiceDate.Year, charge.InvoiceDate.Month))
                .Distinct()
                .OrderBy(month => month);

            foreach (var (year, month) in months)
            {
                EnsureStatement(statements, cardId, year, month, charges);
            }
        }

        var dueDates = cards.Zip(cardIds, (card, cardId) => (DueDate: card["NextInvoiceDueDate"], CardId: cardId))
            .Where(card => card.DueDate is not null)
            .ToList();
        foreach (var (dueDateNode, cardId) in dueDates)
        {
            var dueDate = DateOnly.Parse(dueDateNode!.GetValue<string>());
            EnsureStatement(statements, cardId, dueDate.Year, dueDate.Month, charges)["DueDate"] = dueDateNode.DeepClone();
        }

        root.Remove("CreditCards");

        return $"{cards.Count} cards became accounts; {statements.Count - statementCountBefore} statements added; {dueDates.Count} due dates moved to statements";
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

    private static JsonObject EnsureStatement(JsonArray statements, string cardId, int year, int month, IReadOnlyList<Charge> charges)
    {
        if (StatementFor(statements, cardId, year, month) is { } existing)
        {
            return existing;
        }

        var statement = NewStatement(cardId, year, month, charges);
        statements.Add(statement);
        return statement;
    }

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

    private static bool SameId(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private sealed record Charge(string CardId, DateOnly InvoiceDate, bool IsSettled);
}
