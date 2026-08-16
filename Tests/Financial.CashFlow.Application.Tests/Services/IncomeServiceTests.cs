using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Application.Tests.Services;

public class IncomeServiceTests
{
    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new IncomeService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public async Task AddIncomeAsync_WithValidRequest_SavesAndReturnsIncome()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultIncomeSources: true);
        var service = new IncomeService(repository);

        var result = await service.AddIncomeAsync(ToCreateDto(repository, ValidCreateRequest()));

        using (new AssertionScope())
        {
            result.Date.Should().Be(new DateOnly(2026, 7, 25));
            result.IncomeSourceName.Should().Be("Gleison");
            result.GrossValue.Should().Be(3200.00m);
            result.NetValue.Should().Be(2450.00m);
            result.BankName.Should().Be("Barclays");
            repository.Incomes.Should().ContainSingle();
            repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task AddIncomeAsync_WithoutGrossValue_SavesNull()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultIncomeSources: true);
        var service = new IncomeService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { GrossValue = null });

        var result = await service.AddIncomeAsync(request);

        result.GrossValue.Should().BeNull();
    }

    [Fact]
    public async Task AddIncomeAsync_MultipleEntriesForSameSourceAndMonth_AllPersist()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultIncomeSources: true);
        var service = new IncomeService(repository);
        var request = ValidCreateRequest() with { IncomeSource = "Ariana", GrossValue = null };

        await service.AddIncomeAsync(ToCreateDto(repository, request with { Date = new DateOnly(2026, 7, 1) }));
        await service.AddIncomeAsync(ToCreateDto(repository, request with { Date = new DateOnly(2026, 7, 8) }));
        await service.AddIncomeAsync(ToCreateDto(repository, request with { Date = new DateOnly(2026, 7, 15) }));

        repository.Incomes.Should().HaveCount(3);
    }

    [Fact]
    public async Task AddIncomeAsync_WithNegativeNetValue_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultIncomeSources: true);
        var service = new IncomeService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { GrossValue = null, NetValue = -1m });

        var act = async () => await service.AddIncomeAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddIncomeAsync_WithUnrecognizedIncomeSource_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultIncomeSources: true);
        var service = new IncomeService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { IncomeSource = "NotASource" });

        var act = async () => await service.AddIncomeAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Income source*not recognized*");
    }

    [Fact]
    public async Task AddIncomeAsync_WithInactiveIncomeSource_Succeeds()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        repository.IncomeSources.Add(IncomeSource.Create("RetiredSource", IncomeGroup.NonReportable, isActive: false));
        var service = new IncomeService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { IncomeSource = "RetiredSource" });

        var result = await service.AddIncomeAsync(request);

        result.IncomeSourceName.Should().Be("RetiredSource");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddIncomeAsync_WithBlankIncomeSource_ThrowsArgumentException(string? incomeSource)
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultIncomeSources: true);
        var service = new IncomeService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { IncomeSource = incomeSource! });

        var act = async () => await service.AddIncomeAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddIncomeAsync_WithUnrecognizedBank_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultIncomeSources: true);
        var service = new IncomeService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { Bank = "NotABank" });

        var act = async () => await service.AddIncomeAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Bank*not recognized*");
    }

    [Fact]
    public async Task AddIncomeAsync_WithoutBank_Succeeds()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultIncomeSources: true);
        var service = new IncomeService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { Bank = null });

        var result = await service.AddIncomeAsync(request);

        using (new AssertionScope())
        {
            result.BankId.Should().BeNull();
            result.BankName.Should().BeNull();
        }
    }

    [Fact]
    public async Task AddIncomeAsync_WithDescription_SavesDescription()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultIncomeSources: true);
        var service = new IncomeService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { Description = "Chip ISA dividend" });

        var result = await service.AddIncomeAsync(request);

        result.Description.Should().Be("Chip ISA dividend");
    }

    [Fact]
    public async Task AddIncomeAsync_WithoutDescription_DescriptionIsNull()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultIncomeSources: true);
        var service = new IncomeService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest());

        var result = await service.AddIncomeAsync(request);

        result.Description.Should().BeNull();
    }

    [Fact]
    public async Task AddIncomeAsync_WithDescriptionOver200Characters_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultIncomeSources: true);
        var service = new IncomeService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { Description = new string('a', 201) });

        var act = async () => await service.AddIncomeAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*200 characters*");
    }

    [Fact]
    public async Task AddIncomeAsync_WithDescriptionExactly200Characters_Succeeds()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultIncomeSources: true);
        var service = new IncomeService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { Description = new string('a', 200) });

        var result = await service.AddIncomeAsync(request);

        result.Description.Should().HaveLength(200);
    }

    [Fact]
    public async Task UpdateIncomeAsync_WithExistingId_UpdatesInPlace()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultIncomeSources: true);
        var service = new IncomeService(repository);
        var added = await service.AddIncomeAsync(ToCreateDto(repository, ValidCreateRequest()));

        var updateRequest = ToUpdateDto(repository, ValidCreateRequest() with { NetValue = 500m, GrossValue = null, IncomeSource = "Lottery" });
        var result = await service.UpdateIncomeAsync(added.Id, updateRequest);

        using (new AssertionScope())
        {
            result.Id.Should().Be(added.Id);
            result.NetValue.Should().Be(500m);
            result.IncomeSourceName.Should().Be("Lottery");
            repository.Incomes.Should().ContainSingle();
            repository.SaveChangesCallCount.Should().Be(2);
        }
    }

    [Fact]
    public async Task UpdateIncomeAsync_WithUnrecognizedIncomeSource_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultIncomeSources: true);
        var service = new IncomeService(repository);
        var added = await service.AddIncomeAsync(ToCreateDto(repository, ValidCreateRequest()));

        var updateRequest = ToUpdateDto(repository, ValidCreateRequest() with { IncomeSource = "NotASource" });
        var act = async () => await service.UpdateIncomeAsync(added.Id, updateRequest);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Income source*not recognized*");
    }

    [Fact]
    public async Task UpdateIncomeAsync_RemovingBank_SetsBankNull()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultIncomeSources: true);
        var service = new IncomeService(repository);
        var added = await service.AddIncomeAsync(ToCreateDto(repository, ValidCreateRequest()));

        var updateRequest = ToUpdateDto(repository, ValidCreateRequest() with { Bank = null });
        var result = await service.UpdateIncomeAsync(added.Id, updateRequest);

        using (new AssertionScope())
        {
            result.BankId.Should().BeNull();
            result.BankName.Should().BeNull();
        }
    }

    [Fact]
    public async Task UpdateIncomeAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultIncomeSources: true);
        var service = new IncomeService(repository);

        var act = async () => await service.UpdateIncomeAsync(Guid.NewGuid(), ToUpdateDto(repository, ValidCreateRequest()));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteIncomeAsync_WithExistingId_RemovesAndSaves()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultIncomeSources: true);
        var service = new IncomeService(repository);
        var added = await service.AddIncomeAsync(ToCreateDto(repository, ValidCreateRequest()));

        await service.DeleteIncomeAsync(added.Id);

        repository.Incomes.Should().BeEmpty();
        repository.SaveChangesCallCount.Should().Be(2);
    }

    [Fact]
    public async Task DeleteIncomeAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultIncomeSources: true);
        var service = new IncomeService(repository);

        var act = async () => await service.DeleteIncomeAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetIncomesByMonth_ReturnsOnlyIncomesInThatMonth()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultIncomeSources: true);
        var service = new IncomeService(repository);
        await service.AddIncomeAsync(ToCreateDto(repository, ValidCreateRequest() with { Date = new DateOnly(2026, 7, 10) }));
        await service.AddIncomeAsync(ToCreateDto(repository, ValidCreateRequest() with { Date = new DateOnly(2026, 8, 10) }));

        var result = service.GetIncomesByMonth(2026, 7);

        result.Should().ContainSingle().Which.Date.Should().Be(new DateOnly(2026, 7, 10));
    }

    private static IncomeCreateRequest ValidCreateRequest() => new(
        new DateOnly(2026, 7, 25),
        "Gleison",
        3200.00m,
        2450.00m,
        "Barclays",
        null);

    private static IncomeCreateDTO ToCreateDto(StubCashFlowRepository repository, IncomeCreateRequest r) => new()
    {
        Date = r.Date,
        IncomeSourceId = ResolveIncomeSourceId(repository, r.IncomeSource),
        GrossValue = r.GrossValue,
        NetValue = r.NetValue,
        BankId = ResolveBankId(repository, r.Bank),
        Description = r.Description
    };

    private static IncomeUpdateDTO ToUpdateDto(StubCashFlowRepository repository, IncomeCreateRequest r) => new()
    {
        Date = r.Date,
        IncomeSourceId = ResolveIncomeSourceId(repository, r.IncomeSource),
        GrossValue = r.GrossValue,
        NetValue = r.NetValue,
        BankId = ResolveBankId(repository, r.Bank),
        Description = r.Description
    };

    /// <summary>An unresolvable name maps to a random, never-seeded Guid so tests exercising an unrecognized reference still hit the "not found" path.</summary>
    private static Guid ResolveIncomeSourceId(StubCashFlowRepository repository, string? incomeSourceName) =>
        repository.IncomeSources.FirstOrDefault(s => s.Name == incomeSourceName)?.Id ?? Guid.NewGuid();

    /// <summary>Null bank name means "no bank supplied"; an unresolvable non-null name maps to a random, never-seeded Guid so tests exercising an unrecognized reference still hit the "not found" path.</summary>
    private static Guid? ResolveBankId(StubCashFlowRepository repository, string? bankName) =>
        bankName is null ? null : repository.Banks.FirstOrDefault(b => b.Name == bankName)?.Id ?? Guid.NewGuid();

    private sealed record IncomeCreateRequest(
        DateOnly Date, string IncomeSource, decimal? GrossValue, decimal NetValue, string? Bank, string? Description);

}
