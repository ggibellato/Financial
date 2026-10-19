using System.Text.Json.Nodes;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Infrastructure.Tests.Persistence;

[Trait("Category", "Integration")]
public class TransferDestinationAmountPersistenceTests
{
    private readonly CashFlowSerializerAdapter _serializer = new();
    private readonly Account _barclays = TestAccounts.Bank("Barclays");
    private readonly Account _chase = TestAccounts.Bank("Chase", roundUpEnabled: true);
    private readonly Account _itau = TestAccounts.Bank("Itaú", currency: Currency.BRL);

    private CashFlowData DataWith(params Transfer[] transfers)
    {
        var data = CashFlowData.Create();
        data.AddAccount(_barclays);
        data.AddAccount(_chase);
        data.AddAccount(_itau);
        foreach (var transfer in transfers)
        {
            data.AddTransfer(transfer);
        }

        return data;
    }

    private Transfer SameCurrencyTransfer(string? note) =>
        Transfer.Create(new DateOnly(2026, 9, 10), _barclays, _chase, 150m, destinationAmount: null, note, TestClock.Today);

    private static JsonObject OnlyTransferIn(string json) =>
        JsonNode.Parse(json)!["Transfers"]!.AsArray().Should().ContainSingle().Subject!.AsObject();

    [Fact]
    [Trait("AC", "P60-F03-generic-account-to-account-transfer-07")]
    public void LoadAndSave_SameCurrencyTransfersOnly_FileIsByteIdentical()
    {
        var file = _serializer.Serialize(DataWith(SameCurrencyTransfer("Top-up"), SameCurrencyTransfer(note: null)));

        var saved = _serializer.Serialize(_serializer.Deserialize(file));

        using (new AssertionScope())
        {
            saved.Should().Be(file);
            file.Should().NotContain("DestinationAmount");
        }
    }

    [Fact]
    public void Save_SameCurrencyTransfer_WritesOnlyThePreExistingKeys()
    {
        var file = _serializer.Serialize(DataWith(SameCurrencyTransfer(note: null)));

        OnlyTransferIn(file).Select(property => property.Key).Should().Equal(
            "Id", "Date", "SourceAccountId", "DestinationAccountId", "Amount", "Note");
    }

    [Fact]
    public void LoadAndSave_CrossCurrencyTransfer_WritesTheKeyAfterNoteAndReadsItBack()
    {
        var transfer = Transfer.Create(new DateOnly(2026, 9, 10), _barclays, _itau, 100m, destinationAmount: 640.50m, "Send to Itaú", TestClock.Today);

        var file = _serializer.Serialize(DataWith(transfer));
        var loaded = _serializer.Deserialize(file).Transfers.Should().ContainSingle().Subject;

        using (new AssertionScope())
        {
            OnlyTransferIn(file).Select(property => property.Key).Should().Equal(
                "Id", "Date", "SourceAccountId", "DestinationAccountId", "Amount", "Note", "DestinationAmount");
            OnlyTransferIn(file)["DestinationAmount"]!.GetValue<decimal>().Should().Be(640.50m);
            loaded.DestinationAmount.Should().Be(640.50m);
            loaded.DestinationAccount.Name.Should().Be("Itaú");
        }
    }

    [Fact]
    public void Load_TransferWithoutTheKey_LoadsDestinationAmountAsNull()
    {
        var file = _serializer.Serialize(DataWith(SameCurrencyTransfer("Top-up")));

        var loaded = _serializer.Deserialize(file).Transfers.Should().ContainSingle().Subject;

        loaded.DestinationAmount.Should().BeNull();
    }
}
