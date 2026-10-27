using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Currencies;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Financial.CashFlow.Infrastructure.Persistence;

public sealed class AccountJsonConverter : JsonConverter<Account>
{
    private static readonly JsonSerializerOptions RecordOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly JsonSerializerOptions AccountOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        TypeInfoResolver = new CashFlowTypeInfoResolver()
    };

    public override Account Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var record = JsonSerializer.Deserialize<AccountRecord>(ref reader, RecordOptions)!;

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
            value.Pots?.Select(pot => JsonSerializer.SerializeToElement(pot, AccountOptions)).ToList());

        JsonSerializer.Serialize(writer, record, RecordOptions);
    }

    private sealed record AccountRecord(
        Guid Id,
        string Name,
        AccountTemplate Template,
        Currency Currency,
        bool IsActive,
        OpeningBalanceRecord? OpeningBalance,
        RoundUpRecord? RoundUp,
        List<JsonElement>? Pots);

    private sealed record OpeningBalanceRecord(decimal Amount, DateOnly StartDate);

    private sealed record RoundUpRecord(bool Enabled);
}
