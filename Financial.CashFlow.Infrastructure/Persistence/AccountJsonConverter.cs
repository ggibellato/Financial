using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Currencies;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Financial.CashFlow.Infrastructure.Persistence;

public sealed class AccountJsonConverter : JsonConverter<Account>
{
    private static readonly JsonSerializerOptions AccountOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        TypeInfoResolver = new CashFlowTypeInfoResolver()
    };

    public override Account Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var bank = JsonSerializer.Deserialize<BankRecord>(ref reader)!;

        var identity = new JsonObject
        {
            [nameof(Account.Id)] = bank.Id,
            [nameof(Account.Name)] = bank.Name,
            [nameof(Account.Template)] = nameof(AccountTemplate.Bank),
            [nameof(Account.Currency)] = nameof(Currency.GBP),
            [nameof(Account.IsActive)] = true
        };

        var account = identity.Deserialize<Account>(AccountOptions)!;
        account.SetRoundUp(bank.RoundUpEnabled);
        account.SetOpeningBalance(bank.OpeningBalance, bank.OpeningBalanceDate);
        return account;
    }

    public override void Write(Utf8JsonWriter writer, Account value, JsonSerializerOptions options)
    {
        var bank = new BankRecord(
            value.Id,
            value.Name,
            value.RoundUp?.Enabled ?? false,
            value.OpeningBalance?.Amount ?? 0m,
            value.OpeningBalance?.StartDate ?? default);

        JsonSerializer.Serialize(writer, bank);
    }

    private sealed record BankRecord(Guid Id, string Name, bool RoundUpEnabled, decimal OpeningBalance, DateOnly OpeningBalanceDate);
}
