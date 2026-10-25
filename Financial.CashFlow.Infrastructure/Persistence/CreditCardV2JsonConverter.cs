using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Currencies;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Financial.CashFlow.Infrastructure.Persistence;

public sealed class CreditCardV2JsonConverter : JsonConverter<Account>
{
    public override Account Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var record = JsonSerializer.Deserialize<CreditCardRecord>(ref reader)!;
        var account = AccountJsonConverter.Restore(record.Id, record.Name, AccountTemplate.CreditCard, Currency.GBP, record.IsActive);
        account.SetNextInvoiceDueDate(record.NextInvoiceDueDate);
        return account;
    }

    public override void Write(Utf8JsonWriter writer, Account value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, new CreditCardRecord(value.Id, value.Name, value.IsActive, value.NextInvoiceDueDate));

    private sealed record CreditCardRecord(Guid Id, string Name, bool IsActive, DateOnly? NextInvoiceDueDate);
}
