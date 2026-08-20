using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class TransferServiceTests
{
    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly TransferService _sut;

    public TransferServiceTests()
    {
        _repository = new StubCashFlowRepository(seedDefaultBanks: true);
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    /// <summary>Wires the SUT exactly as the test constructor does, so a test needing a differently
    /// seeded repository does not repeat the whole construction sequence.</summary>
    private TransferService CreateService(StubCashFlowRepository? repository = null) =>
        new(repository ?? _repository, _tracer, NullLogger<TransferService>.Instance);

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new TransferService(null!, _tracer, NullLogger<TransferService>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new TransferService(_repository, null!, NullLogger<TransferService>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public async Task AddTransferAsync_WithValidRequest_SavesAndReturnsTransfer()
    {
        var result = await _sut.AddTransferAsync(ToCreateDto(_repository, ValidCreateRequest()));

        using (new AssertionScope())
        {
            result.Date.Should().Be(new DateOnly(2026, 7, 25));
            result.SourceBankName.Should().Be("Barclays");
            result.DestinationBankName.Should().Be("Trading212");
            result.Amount.Should().Be(500m);
            result.Note.Should().Be("Round-up top-up");
            _repository.Transfers.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task AddTransferAsync_WithoutNote_SavesNull()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { Note = null });

        var result = await _sut.AddTransferAsync(request);

        result.Note.Should().BeNull();
    }

    [Fact]
    public async Task AddTransferAsync_WithSameSourceAndDestinationBank_ThrowsArgumentException()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { DestinationBank = "Barclays" });

        var act = async () => await _sut.AddTransferAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*two different banks*");
    }

    [Fact]
    public async Task AddTransferAsync_WithNonPositiveAmount_ThrowsArgumentException()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { Amount = 0m });

        var act = async () => await _sut.AddTransferAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*greater than zero*");
    }

    [Fact]
    public async Task AddTransferAsync_WithUnresolvableSourceBank_ThrowsArgumentException()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { SourceBank = "NotABank" });

        var act = async () => await _sut.AddTransferAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage($"*Bank '{request.SourceBankId}' was not found*");
    }

    [Fact]
    public async Task AddTransferAsync_WithUnresolvableSourceBank_RecordsFailedSpanWithException()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { SourceBank = "NotABank" });

        var act = async () => await _sut.AddTransferAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
        var span = _tracer.Spans.Should().ContainSingle().Which;
        span.Name.Should().Be("CashFlow.TransferService.AddTransfer");
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Failed);
        span.RecordedException.Should().BeOfType<ArgumentException>();
    }

    [Fact]
    public async Task AddTransferAsync_WithUnresolvableDestinationBank_ThrowsArgumentException()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { DestinationBank = "NotABank" });

        var act = async () => await _sut.AddTransferAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage($"*Bank '{request.DestinationBankId}' was not found*");
    }

    [Fact]
    public async Task UpdateTransferAsync_WithUnresolvableSourceBank_ThrowsArgumentException()
    {
        var added = await _sut.AddTransferAsync(ToCreateDto(_repository, ValidCreateRequest()));
        var updateRequest = ToUpdateDto(_repository, ValidCreateRequest() with { SourceBank = "NotABank" });

        var act = async () => await _sut.UpdateTransferAsync(added.Id, updateRequest);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage($"*Bank '{updateRequest.SourceBankId}' was not found*");
    }

    [Fact]
    public async Task UpdateTransferAsync_WithExistingId_UpdatesInPlace()
    {
        var added = await _sut.AddTransferAsync(ToCreateDto(_repository, ValidCreateRequest()));

        var updateRequest = ToUpdateDto(_repository, ValidCreateRequest() with { Amount = 250m, SourceBank = "Chase", Note = "Updated" });
        var result = await _sut.UpdateTransferAsync(added.Id, updateRequest);

        using (new AssertionScope())
        {
            result.Id.Should().Be(added.Id);
            result.Amount.Should().Be(250m);
            result.SourceBankName.Should().Be("Chase");
            result.Note.Should().Be("Updated");
            _repository.Transfers.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(2);
        }
    }

    [Fact]
    public async Task UpdateTransferAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.UpdateTransferAsync(Guid.NewGuid(), ToUpdateDto(_repository, ValidCreateRequest()));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteTransferAsync_WithExistingId_RemovesAndSaves()
    {
        var added = await _sut.AddTransferAsync(ToCreateDto(_repository, ValidCreateRequest()));

        await _sut.DeleteTransferAsync(added.Id);

        _repository.Transfers.Should().BeEmpty();
        _repository.SaveChangesCallCount.Should().Be(2);
    }

    [Fact]
    public async Task DeleteTransferAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.DeleteTransferAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetTransfersByMonth_ReturnsOnlyTransfersInThatMonth()
    {
        await _sut.AddTransferAsync(ToCreateDto(_repository, ValidCreateRequest() with { Date = new DateOnly(2026, 7, 10) }));
        await _sut.AddTransferAsync(ToCreateDto(_repository, ValidCreateRequest() with { Date = new DateOnly(2026, 8, 10) }));

        var result = _sut.GetTransfersByMonth(2026, 7);

        result.Should().ContainSingle().Which.Date.Should().Be(new DateOnly(2026, 7, 10));
    }

    [Fact]
    public async Task GetTransfersByBank_ReturnsTransfersWhereBankIsSourceOrDestination()
    {
        await _sut.AddTransferAsync(ToCreateDto(_repository, ValidCreateRequest() with { SourceBank = "Barclays", DestinationBank = "Trading212" }));
        await _sut.AddTransferAsync(ToCreateDto(_repository, ValidCreateRequest() with { SourceBank = "Chase", DestinationBank = "Barclays" }));
        await _sut.AddTransferAsync(ToCreateDto(_repository, ValidCreateRequest() with { SourceBank = "Chase", DestinationBank = "Trading212" }));

        var result = _sut.GetTransfersByBank(_repository.Banks.First(b => b.Name == "Barclays").Id);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void GetTransfersByBank_WithUnrecognizedBank_ReturnsEmptyList()
    {
        var result = _sut.GetTransfersByBank(Guid.NewGuid());

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

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new TransferService(_repository, _tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
