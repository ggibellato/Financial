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

        var result = await service.AddTransferAsync(ToCreateDto(ValidCreateRequest()));

        using (new AssertionScope())
        {
            result.Date.Should().Be(new DateOnly(2026, 7, 25));
            result.SourceBank.Should().Be("Barclays");
            result.DestinationBank.Should().Be("Trading212");
            result.Amount.Should().Be(500m);
            result.Note.Should().Be("Round-up top-up");
            repository.Transfers.Should().ContainSingle();
            repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task AddTransferAsync_WithoutNote_SavesNull()
    {
        var service = new TransferService(new StubCashFlowRepository(seedDefaultBanks: true));
        var request = ToCreateDto(ValidCreateRequest() with { Note = null });

        var result = await service.AddTransferAsync(request);

        result.Note.Should().BeNull();
    }

    [Fact]
    public async Task AddTransferAsync_WithSameSourceAndDestinationBank_ThrowsArgumentException()
    {
        var service = new TransferService(new StubCashFlowRepository(seedDefaultBanks: true));
        var request = ToCreateDto(ValidCreateRequest() with { DestinationBank = "Barclays" });

        var act = async () => await service.AddTransferAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*two different banks*");
    }

    [Fact]
    public async Task AddTransferAsync_WithNonPositiveAmount_ThrowsArgumentException()
    {
        var service = new TransferService(new StubCashFlowRepository(seedDefaultBanks: true));
        var request = ToCreateDto(ValidCreateRequest() with { Amount = 0m });

        var act = async () => await service.AddTransferAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*greater than zero*");
    }

    [Fact]
    public async Task AddTransferAsync_WithUnresolvableSourceBank_ThrowsArgumentException()
    {
        var service = new TransferService(new StubCashFlowRepository(seedDefaultBanks: true));
        var request = ToCreateDto(ValidCreateRequest() with { SourceBank = "NotABank" });

        var act = async () => await service.AddTransferAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Bank 'NotABank' was not found*");
    }

    [Fact]
    public async Task AddTransferAsync_WithUnresolvableDestinationBank_ThrowsArgumentException()
    {
        var service = new TransferService(new StubCashFlowRepository(seedDefaultBanks: true));
        var request = ToCreateDto(ValidCreateRequest() with { DestinationBank = "NotABank" });

        var act = async () => await service.AddTransferAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Bank 'NotABank' was not found*");
    }

    [Fact]
    public async Task UpdateTransferAsync_WithExistingId_UpdatesInPlace()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new TransferService(repository);
        var added = await service.AddTransferAsync(ToCreateDto(ValidCreateRequest()));

        var updateRequest = ToUpdateDto(ValidCreateRequest() with { Amount = 250m, SourceBank = "Chase", Note = "Updated" });
        var result = await service.UpdateTransferAsync(added.Id, updateRequest);

        using (new AssertionScope())
        {
            result.Id.Should().Be(added.Id);
            result.Amount.Should().Be(250m);
            result.SourceBank.Should().Be("Chase");
            result.Note.Should().Be("Updated");
            repository.Transfers.Should().ContainSingle();
            repository.SaveChangesCallCount.Should().Be(2);
        }
    }

    [Fact]
    public async Task UpdateTransferAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new TransferService(new StubCashFlowRepository(seedDefaultBanks: true));

        var act = async () => await service.UpdateTransferAsync(Guid.NewGuid(), ToUpdateDto(ValidCreateRequest()));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteTransferAsync_WithExistingId_RemovesAndSaves()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new TransferService(repository);
        var added = await service.AddTransferAsync(ToCreateDto(ValidCreateRequest()));

        await service.DeleteTransferAsync(added.Id);

        repository.Transfers.Should().BeEmpty();
        repository.SaveChangesCallCount.Should().Be(2);
    }

    [Fact]
    public async Task DeleteTransferAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new TransferService(new StubCashFlowRepository(seedDefaultBanks: true));

        var act = async () => await service.DeleteTransferAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetTransfersByMonth_ReturnsOnlyTransfersInThatMonth()
    {
        var service = new TransferService(new StubCashFlowRepository(seedDefaultBanks: true));
        await service.AddTransferAsync(ToCreateDto(ValidCreateRequest() with { Date = new DateOnly(2026, 7, 10) }));
        await service.AddTransferAsync(ToCreateDto(ValidCreateRequest() with { Date = new DateOnly(2026, 8, 10) }));

        var result = service.GetTransfersByMonth(2026, 7);

        result.Should().ContainSingle().Which.Date.Should().Be(new DateOnly(2026, 7, 10));
    }

    [Fact]
    public async Task GetTransfersByBank_ReturnsTransfersWhereBankIsSourceOrDestination()
    {
        var service = new TransferService(new StubCashFlowRepository(seedDefaultBanks: true));
        await service.AddTransferAsync(ToCreateDto(ValidCreateRequest() with { SourceBank = "Barclays", DestinationBank = "Trading212" }));
        await service.AddTransferAsync(ToCreateDto(ValidCreateRequest() with { SourceBank = "Chase", DestinationBank = "Barclays" }));
        await service.AddTransferAsync(ToCreateDto(ValidCreateRequest() with { SourceBank = "Chase", DestinationBank = "Trading212" }));

        var result = service.GetTransfersByBank("Barclays");

        result.Should().HaveCount(2);
    }

    [Fact]
    public void GetTransfersByBank_WithUnrecognizedBank_ReturnsEmptyList()
    {
        var service = new TransferService(new StubCashFlowRepository(seedDefaultBanks: true));

        var result = service.GetTransfersByBank("NotABank");

        result.Should().BeEmpty();
    }

    private static TransferCreateRequest ValidCreateRequest() => new(
        new DateOnly(2026, 7, 25),
        "Barclays",
        "Trading212",
        500m,
        "Round-up top-up");

    private static TransferCreateDTO ToCreateDto(TransferCreateRequest r) => new()
    {
        Date = r.Date,
        SourceBank = r.SourceBank,
        DestinationBank = r.DestinationBank,
        Amount = r.Amount,
        Note = r.Note
    };

    private static TransferUpdateDTO ToUpdateDto(TransferCreateRequest r) => new()
    {
        Date = r.Date,
        SourceBank = r.SourceBank,
        DestinationBank = r.DestinationBank,
        Amount = r.Amount,
        Note = r.Note
    };

    private sealed record TransferCreateRequest(
        DateOnly Date, string SourceBank, string DestinationBank, decimal Amount, string? Note);

}
