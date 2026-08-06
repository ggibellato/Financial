using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Application.Tests.TestHelpers;
using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Application.Tests.Services;

public class TransferServiceTests
{
    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new TransferService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public async Task AddTransferAsync_WithValidRequest_SavesAndReturnsTransfer()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new TransferService(repository);

        var result = await service.AddTransferAsync(ToCreateDto(repository, ValidCreateRequest()));

        using (new AssertionScope())
        {
            result.Date.Should().Be(new DateOnly(2026, 7, 25));
            result.SourceBankName.Should().Be("Barclays");
            result.DestinationBankName.Should().Be("Trading212");
            result.Amount.Should().Be(500m);
            result.Note.Should().Be("Round-up top-up");
            repository.Transfers.Should().ContainSingle();
            repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task AddTransferAsync_WithoutNote_SavesNull()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new TransferService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { Note = null });

        var result = await service.AddTransferAsync(request);

        result.Note.Should().BeNull();
    }

    [Fact]
    public async Task AddTransferAsync_WithSameSourceAndDestinationBank_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new TransferService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { DestinationBank = "Barclays" });

        var act = async () => await service.AddTransferAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*two different banks*");
    }

    [Fact]
    public async Task AddTransferAsync_WithNonPositiveAmount_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new TransferService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { Amount = 0m });

        var act = async () => await service.AddTransferAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*greater than zero*");
    }

    [Fact]
    public async Task AddTransferAsync_WithUnresolvableSourceBank_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new TransferService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { SourceBank = "NotABank" });

        var act = async () => await service.AddTransferAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage($"*Bank '{request.SourceBankId}' was not found*");
    }

    [Fact]
    public async Task AddTransferAsync_WithUnresolvableDestinationBank_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new TransferService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { DestinationBank = "NotABank" });

        var act = async () => await service.AddTransferAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage($"*Bank '{request.DestinationBankId}' was not found*");
    }

    [Fact]
    public async Task UpdateTransferAsync_WithUnresolvableSourceBank_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new TransferService(repository);
        var added = await service.AddTransferAsync(ToCreateDto(repository, ValidCreateRequest()));
        var updateRequest = ToUpdateDto(repository, ValidCreateRequest() with { SourceBank = "NotABank" });

        var act = async () => await service.UpdateTransferAsync(added.Id, updateRequest);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage($"*Bank '{updateRequest.SourceBankId}' was not found*");
    }

    [Fact]
    public async Task UpdateTransferAsync_WithExistingId_UpdatesInPlace()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new TransferService(repository);
        var added = await service.AddTransferAsync(ToCreateDto(repository, ValidCreateRequest()));

        var updateRequest = ToUpdateDto(repository, ValidCreateRequest() with { Amount = 250m, SourceBank = "Chase", Note = "Updated" });
        var result = await service.UpdateTransferAsync(added.Id, updateRequest);

        using (new AssertionScope())
        {
            result.Id.Should().Be(added.Id);
            result.Amount.Should().Be(250m);
            result.SourceBankName.Should().Be("Chase");
            result.Note.Should().Be("Updated");
            repository.Transfers.Should().ContainSingle();
            repository.SaveChangesCallCount.Should().Be(2);
        }
    }

    [Fact]
    public async Task UpdateTransferAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new TransferService(repository);

        var act = async () => await service.UpdateTransferAsync(Guid.NewGuid(), ToUpdateDto(repository, ValidCreateRequest()));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteTransferAsync_WithExistingId_RemovesAndSaves()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new TransferService(repository);
        var added = await service.AddTransferAsync(ToCreateDto(repository, ValidCreateRequest()));

        await service.DeleteTransferAsync(added.Id);

        repository.Transfers.Should().BeEmpty();
        repository.SaveChangesCallCount.Should().Be(2);
    }

    [Fact]
    public async Task DeleteTransferAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new TransferService(repository);

        var act = async () => await service.DeleteTransferAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetTransfersByMonth_ReturnsOnlyTransfersInThatMonth()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new TransferService(repository);
        await service.AddTransferAsync(ToCreateDto(repository, ValidCreateRequest() with { Date = new DateOnly(2026, 7, 10) }));
        await service.AddTransferAsync(ToCreateDto(repository, ValidCreateRequest() with { Date = new DateOnly(2026, 8, 10) }));

        var result = service.GetTransfersByMonth(2026, 7);

        result.Should().ContainSingle().Which.Date.Should().Be(new DateOnly(2026, 7, 10));
    }

    [Fact]
    public async Task GetTransfersByBank_ReturnsTransfersWhereBankIsSourceOrDestination()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new TransferService(repository);
        await service.AddTransferAsync(ToCreateDto(repository, ValidCreateRequest() with { SourceBank = "Barclays", DestinationBank = "Trading212" }));
        await service.AddTransferAsync(ToCreateDto(repository, ValidCreateRequest() with { SourceBank = "Chase", DestinationBank = "Barclays" }));
        await service.AddTransferAsync(ToCreateDto(repository, ValidCreateRequest() with { SourceBank = "Chase", DestinationBank = "Trading212" }));

        var result = service.GetTransfersByBank(repository.Banks.First(b => b.Name == "Barclays").Id);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void GetTransfersByBank_WithUnrecognizedBank_ReturnsEmptyList()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new TransferService(repository);

        var result = service.GetTransfersByBank(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    private static TransferCreateRequest ValidCreateRequest() => new(
        new DateOnly(2026, 7, 25),
        "Barclays",
        "Trading212",
        500m,
        "Round-up top-up");

    private static TransferCreateDTO ToCreateDto(StubCashFlowRepository repository, TransferCreateRequest r) => new()
    {
        Date = r.Date,
        SourceBankId = ResolveBankId(repository, r.SourceBank),
        DestinationBankId = ResolveBankId(repository, r.DestinationBank),
        Amount = r.Amount,
        Note = r.Note
    };

    private static TransferUpdateDTO ToUpdateDto(StubCashFlowRepository repository, TransferCreateRequest r) => new()
    {
        Date = r.Date,
        SourceBankId = ResolveBankId(repository, r.SourceBank),
        DestinationBankId = ResolveBankId(repository, r.DestinationBank),
        Amount = r.Amount,
        Note = r.Note
    };

    /// <summary>An unresolvable name maps to a random, never-seeded Guid so tests exercising an unrecognized reference still hit the "not found" path.</summary>
    private static Guid ResolveBankId(StubCashFlowRepository repository, string? bankName) =>
        repository.Banks.FirstOrDefault(b => b.Name == bankName)?.Id ?? Guid.NewGuid();

    private sealed record TransferCreateRequest(
        DateOnly Date, string SourceBank, string DestinationBank, decimal Amount, string? Note);

}
