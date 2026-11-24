using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.ValueObjects;
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

        if (record.Cashback is { } cashback)
        {
            account.SetCashback(CashbackSetting.On(cashback.RatePercent));
        }

        if (account.Has(Capability.HasPots) != record.Pots is not null)
        {
            throw new JsonException($"Account {record.Id} must have Pots exactly when its template has pots.");
        }

        foreach (var pot in record.Pots ?? [])
        {
            account.AddPot(pot.Deserialize<Pot>(AccountOptions)!);
        }

        if (record.Category is { } category)
        {
            account.SetCategory(category);
        }

        ApplyCurrencies(account, record);
        ApplyOtherParticipant(account, record);
        ApplySavings(account, record);
        DeferDestinations(account, record);

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
            value.Category,
            value.Has(Capability.MultiCurrency) ? [.. value.Currencies] : null,
            value.OtherParticipant,
            value.Savings is { } savings ? ToRecord(savings) : null,
            value.Cashback?.RatePercent is { } cashbackRate ? new CashbackRecord(cashbackRate) : null,
            ToRecord(value.RoundUpDestination),
            ToRecord(value.CashbackDestination));

        JsonSerializer.Serialize(writer, record, _recordOptions);
    }

    private static void ApplyCurrencies(Account account, AccountRecord record)
    {
        if (account.Has(Capability.MultiCurrency) != record.Currencies is not null)
        {
            throw new JsonException($"Account {record.Id} must have Currencies exactly when its template has several currencies.");
        }

        if (record.Currencies is not { } currencies)
        {
            return;
        }

        account.SetCurrencies(currencies);
        if (account.Currency != record.Currency)
        {
            throw new JsonException($"Account {record.Id} Currency must be the first of its Currencies.");
        }
    }

    private static void ApplyOtherParticipant(Account account, AccountRecord record)
    {
        if (record.OtherParticipant is not { } otherParticipant)
        {
            return;
        }

        if (!account.Has(Capability.HasParticipants))
        {
            throw new JsonException($"Account {record.Id} can name another participant only when its template has participants.");
        }

        account.SetOtherParticipant(otherParticipant);
    }

    private static void ApplySavings(Account account, AccountRecord record)
    {
        if (account.Has(Capability.EarnsInterest) != record.Savings is not null)
        {
            throw new JsonException($"Account {record.Id} must have Savings exactly when its template earns interest.");
        }

        if (record.Savings is not { } savings)
        {
            return;
        }

        account.SetSavings(SavingsTerms.Create(
            savings.Frequency,
            savings.PaymentDay,
            savings.IsTaxFree,
            savings.Rates.Select(rate => InterestRate.Create(rate.AnnualPercent, rate.EffectiveFrom)),
            savings.RoundUpOnly));
    }

    private void DeferDestinations(Account account, AccountRecord record)
    {
        if (record.RoundUpDestination is null && record.CashbackDestination is null)
        {
            return;
        }

        if (context is null)
        {
            throw new JsonException($"Account {record.Id} has a destination that can only be read with the other accounts.");
        }

        context.PendingDestinations.Add((account, ToDestination(record.RoundUpDestination), ToDestination(record.CashbackDestination)));
    }

    private static MoneyDestination? ToDestination(DestinationRecord? record) => record switch
    {
        null => null,
        { Kind: MoneyDestinationKind.Account, AccountId: { } accountId } => MoneyDestination.ToAccount(accountId),
        { Kind: MoneyDestinationKind.InvestedAutomatically } => MoneyDestination.InvestedAutomatically,
        _ => throw new JsonException($"Unrecognised destination {record.Kind}.")
    };

    private static DestinationRecord? ToRecord(MoneyDestination? destination) =>
        destination is null || destination.Kind == MoneyDestinationKind.None ? null : new DestinationRecord(destination.Kind, destination.AccountId);

    private static SavingsRecord ToRecord(SavingsTerms savings) =>
        new(
            savings.Frequency,
            savings.PaymentDay,
            savings.IsTaxFree,
            savings.Rates.Select(rate => new InterestRateRecord(rate.AnnualPercent, rate.EffectiveFrom)).ToList(),
            savings.RoundUpOnly);

    private sealed record AccountRecord(
        Guid Id,
        string Name,
        AccountTemplate Template,
        Currency Currency,
        bool IsActive,
        OpeningBalanceRecord? OpeningBalance,
        RoundUpRecord? RoundUp,
        List<JsonElement>? Pots,
        [property: JsonPropertyName("CategoryId")] Category? Category,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] List<Currency>? Currencies,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? OtherParticipant,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] SavingsRecord? Savings,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] CashbackRecord? Cashback,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DestinationRecord? RoundUpDestination,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DestinationRecord? CashbackDestination);

    private sealed record OpeningBalanceRecord(decimal Amount, DateOnly StartDate);

    private sealed record RoundUpRecord(bool Enabled);

    private sealed record CashbackRecord(decimal RatePercent);

    private sealed record DestinationRecord(
        MoneyDestinationKind Kind,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? AccountId);

    private sealed record SavingsRecord(
        InterestFrequency Frequency,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? PaymentDay,
        bool IsTaxFree,
        List<InterestRateRecord> Rates,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool RoundUpOnly = false);

    private sealed record InterestRateRecord(decimal AnnualPercent, DateOnly EffectiveFrom);
}
