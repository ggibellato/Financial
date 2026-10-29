using System.Text.Json.Nodes;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.DataFileMigration;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlowSpreadsheetImport.Tests.DataFileMigration;

[Trait("Category", "Unit")]
public class V3ToV4PotsAccountsTests
{
    private const string BarclaysId = "6a0f0000-0000-0000-0000-000000000001";
    private const string AmexId = "6a0f0000-0000-0000-0000-000000000002";
    private const string InvestimentoId = "6a0f0000-0000-0000-0000-000000000101";
    private const string ArianaId = "6a0f0000-0000-0000-0000-000000000102";
    private const string MovementId = "6a0f0000-0000-0000-0000-000000000201";
    private const string MercadoId = "6a0f0000-0000-0000-0000-000000000301";
    private const string ReservaCategoryId = "6a0f0000-0000-0000-0000-000000000302";
    private const string ReservasPessoaisId = "6a0f0000-0000-0000-0000-000000000401";
    private const string AmexBalanceId = "6a0f0000-0000-0000-0000-000000000402";
    private const string ArianaSourceId = "6a0f0000-0000-0000-0000-000000000501";
    private const string GleisonSourceId = "6a0f0000-0000-0000-0000-000000000502";
    private const string LotterySourceId = "6a0f0000-0000-0000-0000-000000000503";

    private const string V3Json = $$"""
        {
          "ReserveBuckets": [
            { "Id": "{{InvestimentoId}}", "Name": "Investimento", "IsActive": true, "SplitPercentage": 33.33 },
            { "Id": "{{ArianaId}}", "Name": "Ariana", "IsActive": false, "SplitPercentage": 0 }
          ],
          "ReserveMovements": [
            { "Id": "{{MovementId}}", "BucketId": "{{ArianaId}}", "Amount": 75, "Date": "2026-07-01", "Description": "Deposit", "IncomeId": null }
          ],
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 100, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{AmexId}}", "Name": "BA Amex", "Template": "CreditCard", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null }
          ],
          "Categories": [
            { "Id": "{{MercadoId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false },
            { "Id": "{{ReservaCategoryId}}", "Name": "reserva", "Active": false, "IsInvestment": false, "IsTithe": false }
          ],
          "InvestmentAccounts": [
            { "Id": "{{ReservasPessoaisId}}", "Name": "Reservas pessoais", "IsActive": true, "IsLiability": false, "Source": "ReserveBucketsSum", "CreditCardId": null },
            { "Id": "{{AmexBalanceId}}", "Name": "Amex balance", "IsActive": true, "IsLiability": true, "Source": "CreditCard", "CreditCardId": "{{AmexId}}" }
          ],
          "IncomeSources": [
            { "Id": "{{ArianaSourceId}}", "Name": "Ariana", "IsActive": true, "Group": "Salary", "AutoSplitToReserve": true },
            { "Id": "{{GleisonSourceId}}", "Name": "Gleison", "IsActive": true, "Group": "Salary", "AutoSplitToReserve": false },
            { "Id": "{{LotterySourceId}}", "Name": "Lottery", "IsActive": true, "Group": "NonReportable" }
          ],
          "Version": 3
        }
        """;

    private readonly V3ToV4PotsAccounts _sut = new();

    private static JsonObject Root(string json = V3Json) => JsonNode.Parse(json)!.AsObject();

    private static List<JsonObject> Accounts(JsonObject root) => root["Accounts"]!.AsArray().Select(a => a!.AsObject()).ToList();

    [Fact]
    public void FromVersion_Is3()
    {
        _sut.FromVersion.Should().Be(3);
    }

    [Fact]
    [Trait("AC", "P60-F09-pots-template-and-reserva-migration-01")]
    public void Apply_Buckets_BecomePotsOfReservaWithSameFields()
    {
        var root = Root();
        var bucketsBefore = root["ReserveBuckets"]!.AsArray().Select(b => b!.ToJsonString()).ToList();

        var summary = _sut.Apply(root);

        var reserva = Accounts(root).Last();
        using (new AssertionScope())
        {
            root.ContainsKey("ReserveBuckets").Should().BeFalse();
            Accounts(root).Should().HaveCount(3);
            Guid.TryParse((string)reserva["Id"]!, out _).Should().BeTrue();
            reserva.ToJsonString().Should().Be(
                $$$"""{"Id":"{{{reserva["Id"]}}}","Name":"Reserva","Template":"Pots","Currency":"GBP","IsActive":true,"OpeningBalance":null,"RoundUp":null,"Pots":[{{{string.Join(",", bucketsBefore)}}}],"CategoryId":"{{{ReservaCategoryId}}}"}""");
            summary.Should().Be("Reserva Pots account created with 2 pots; category Reserva; 1 snapshot sources and 1 income sources point at it");
        }
    }

    [Fact]
    [Trait("AC", "P60-F09-pots-template-and-reserva-migration-03")]
    public void Apply_PotsSourcedInvestmentAccount_NamesReservaAndOthersGetNull()
    {
        var root = Root();

        _sut.Apply(root);

        var reservaId = (string)Accounts(root).Last()["Id"]!;
        root["InvestmentAccounts"]!.AsArray().Select(account => account!.ToJsonString()).Should().Equal(
            $$$"""{"Id":"{{{ReservasPessoaisId}}}","Name":"Reservas pessoais","IsActive":true,"IsLiability":false,"Source":"PotsAccountTotal","CreditCardId":null,"PotsAccountId":"{{{reservaId}}}"}""",
            $$$"""{"Id":"{{{AmexBalanceId}}}","Name":"Amex balance","IsActive":true,"IsLiability":true,"Source":"CreditCard","CreditCardId":"{{{AmexId}}}","PotsAccountId":null}""");
    }

    [Fact]
    [Trait("AC", "P60-F09-pots-template-and-reserva-migration-01")]
    public void Apply_AutoSplitIncomeSources_NameReservaAndLoseTheFlag()
    {
        var root = Root();

        _sut.Apply(root);

        var reservaId = (string)Accounts(root).Last()["Id"]!;
        root["IncomeSources"]!.AsArray().Select(source => source!.ToJsonString()).Should().Equal(
            $$$"""{"Id":"{{{ArianaSourceId}}}","Name":"Ariana","IsActive":true,"Group":"Salary","PotsAccountId":"{{{reservaId}}}"}""",
            $$$"""{"Id":"{{{GleisonSourceId}}}","Name":"Gleison","IsActive":true,"Group":"Salary","PotsAccountId":null}""",
            $$$"""{"Id":"{{{LotterySourceId}}}","Name":"Lottery","IsActive":true,"Group":"NonReportable","PotsAccountId":null}""");
    }

    [Fact]
    [Trait("AC", "P60-F09-pots-template-and-reserva-migration-01")]
    public void Apply_OtherAccountsGetNullPotsAndCategory_AndMovementsAreUnchanged()
    {
        var root = Root();
        var movementsBefore = root["ReserveMovements"]!.ToJsonString();

        _sut.Apply(root);

        using (new AssertionScope())
        {
            Accounts(root).Take(2).Should().OnlyContain(account =>
                account.ContainsKey("Pots") && account["Pots"] == null && account.ContainsKey("CategoryId") && account["CategoryId"] == null);
            root["ReserveMovements"]!.ToJsonString().Should().Be(movementsBefore);
        }
    }

    [Fact]
    public void Apply_NoBuckets_CreatesAnEmptyReserva()
    {
        var root = Root($$"""{ "Accounts": [], "Categories": [{ "Id": "{{ReservaCategoryId}}", "Name": "Reserva" }], "Version": 3 }""");

        var summary = _sut.Apply(root);

        using (new AssertionScope())
        {
            Accounts(root).Should().ContainSingle().Which["Pots"]!.AsArray().Should().BeEmpty();
            summary.Should().Be("Reserva Pots account created with 0 pots; category Reserva; 0 snapshot sources and 0 income sources point at it");
        }
    }

    [Fact]
    [Trait("AC", "P60-F09-pots-template-and-reserva-migration-01")]
    public void Apply_ReservaCategory_IsSetFromTheFirstCategoryNamedReservaIgnoringCase()
    {
        var root = Root(V3Json.Replace(
            $"{{ \"Id\": \"{MercadoId}\", \"Name\": \"Mercado\"",
            $"{{ \"Id\": \"{MercadoId}\", \"Name\": \"RESERVA\""));

        _sut.Apply(root);

        Accounts(root).Last()["CategoryId"]!.GetValue<string>().Should().Be(MercadoId);
    }

    [Fact]
    public void Apply_NoCategoryNamedReserva_IsRefused()
    {
        var root = Root(V3Json.Replace("\"Name\": \"reserva\"", "\"Name\": \"Savings\""));
        var before = root.ToJsonString();

        var act = () => _sut.Apply(root);

        using (new AssertionScope())
        {
            act.Should().Throw<InvalidDataException>().WithMessage("No category named \"Reserva\" to assign to the Reserva Pots account.");
            root.ToJsonString().Should().Be(before);
        }
    }

    [Fact]
    public void Apply_FileThatAlreadyHasAPotsAccount_IsRefused()
    {
        var root = Root(V3Json.Replace("\"Name\": \"BA Amex\", \"Template\": \"CreditCard\"", "\"Name\": \"Holiday pots\", \"Template\": \"Pots\""));
        var before = root.ToJsonString();

        var act = () => _sut.Apply(root);

        using (new AssertionScope())
        {
            act.Should().Throw<InvalidDataException>()
                .WithMessage("'Holiday pots' is a Pots account; Pots accounts start in V4. Remove it and migrate again.");
            root.ToJsonString().Should().Be(before);
        }
    }

    [Theory]
    [InlineData("Reserva")]
    [InlineData("RESERVA")]
    public void Apply_AccountAlreadyNamedReserva_IsRefused(string name)
    {
        var root = Root(V3Json.Replace("\"Name\": \"Barclays\"", $"\"Name\": \"{name}\""));
        var before = root.ToJsonString();

        var act = () => _sut.Apply(root);

        using (new AssertionScope())
        {
            act.Should().Throw<InvalidDataException>().WithMessage("An account named \"Reserva\" already exists.");
            root.ToJsonString().Should().Be(before);
        }
    }

    [Fact]
    public void Apply_MovementOfAnUnknownBucket_IsRefused()
    {
        const string unknownId = "6a0f0000-0000-0000-0000-000000000999";
        var root = Root(V3Json.Replace($"\"BucketId\": \"{ArianaId}\"", $"\"BucketId\": \"{unknownId}\""));
        var before = root.ToJsonString();

        var act = () => _sut.Apply(root);

        using (new AssertionScope())
        {
            act.Should().Throw<InvalidDataException>().WithMessage($"ReserveMovement {MovementId} references unknown bucket {unknownId}");
            root.ToJsonString().Should().Be(before);
        }
    }
}
