using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Currencies;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Financial.CashFlow.Infrastructure.Persistence;

public sealed class AccountJsonConverter(ReferenceResolutionContext? context = null) : JsonConverter<Account>
{
    private readonly JsonSerializerOptions _recordOptions = new()
    {
        Converters = { new JsonStringEnumConverter(), new CategoryReferenceConverter(context?.Categories) }
    };

    private static readonly JsonSerializerOptions AccountOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        TypeInfoResolver = new CashFlowTypeInfoResolver()
    };

    public override Account Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var record = JsonSerializer.Deserialize<AccountRecord>(ref reader, _recordOptions)!;

        var identity = new JsonObject
        {
            [nameof(Account.Id)] = record.Id,
            [nameof(Account.Name)] = record.Name,
            [nameof(Account.Template)] = record.Template.ToString(),
            [nameof(Account.Currency)] = record.Currency.ToString(),
            [nameof(Account.IsActive)] = record.IsActive
        };

        var account = identity.Deserialize<Account>(AccountOptions)!;

        if (record.OpeningBalance is { } openingBalance)
        {
            account.SetOpeningBalance(openingBalance.Amount, openingBalance.StartDate);
        }

        if (record.RoundUp is { } roundUp)
        {
            account.SetRoundUp(roundUp.Enabled);
        }

        if (account.Has(Capability.HasPots) != record.Pots is not null)
        {
            throw new JsonException($"Account {record.Id} must have Pots exactly when its template has pots.");
        }

        foreach (var pot in record.Pots ?? [])
        {
            account.AddPot(pot.Deserialize<ReserveBucket>(AccountOptions)!);
        }

        if (record.Category is { } category)
        {
            account.SetCategory(category);
        }

        return account;
    }

    public override void Write(Utf8JsonWriter writer, Account value, JsonSerializerOptions options)
    {
        var record = new AccountRecord(
            value.Id,
            value.Name,
            value.Template,
            value.Currency,
            value.IsActive,
            value.OpeningBalance is { } openingBalance ? new OpeningBalanceRecord(openingBalance.Amount, openingBalance.StartDate) : null,
            value.RoundUp is { } roundUp ? new RoundUpRecord(roundUp.Enabled) : null,
            value.Pots?.Select(pot => JsonSerializer.SerializeToElement(pot, AccountOptions)).ToList(),
            value.Category);

        JsonSerializer.Serialize(writer, record, _recordOptions);
    }

    private sealed record AccountRecord(
        Guid Id,
        string Name,
        AccountTemplate Template,
        Currency Currency,
        bool IsActive,
        OpeningBalanceRecord? OpeningBalance,
        RoundUpRecord? RoundUp,
        List<JsonElement>? Pots,
        [property: JsonPropertyName("CategoryId")] Category? Category);

    private sealed record OpeningBalanceRecord(decimal Amount, DateOnly StartDate);

    private sealed record RoundUpRecord(bool Enabled);
}
