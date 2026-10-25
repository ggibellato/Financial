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

        var account = Restore(record.Id, record.Name, record.Template, record.Currency, record.IsActive);

        if (record.OpeningBalance is { } openingBalance)
        {
            account.SetOpeningBalance(openingBalance.Amount, openingBalance.StartDate);
        }

        if (record.RoundUp is { } roundUp)
        {
            account.SetRoundUp(roundUp.Enabled);
        }

        return account;
    }

    internal static Account Restore(Guid id, string name, AccountTemplate template, Currency currency, bool isActive)
    {
        var identity = new JsonObject
        {
            [nameof(Account.Id)] = id,
            [nameof(Account.Name)] = name,
            [nameof(Account.Template)] = template.ToString(),
            [nameof(Account.Currency)] = currency.ToString(),
            [nameof(Account.IsActive)] = isActive
        };

        return identity.Deserialize<Account>(AccountOptions)!;
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
            value.RoundUp is { } roundUp ? new RoundUpRecord(roundUp.Enabled) : null);

        JsonSerializer.Serialize(writer, record, RecordOptions);
    }

    private sealed record AccountRecord(
        Guid Id,
        string Name,
        AccountTemplate Template,
        Currency Currency,
        bool IsActive,
        OpeningBalanceRecord? OpeningBalance,
        RoundUpRecord? RoundUp);

    private sealed record OpeningBalanceRecord(decimal Amount, DateOnly StartDate);

    private sealed record RoundUpRecord(bool Enabled);
}
