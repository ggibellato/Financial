using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace Financial.CashFlow.Application.Tests.Services;

[Trait("Category", "Unit")]
public class TransferServiceTests
{
    private readonly StubCashFlowRepository _repository;
    private readonly FakeTimeProvider _clock;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly RecordingLogger<TransferService> _logger;
    private readonly TransferService _sut;

    public TransferServiceTests()
    {
        _repository = new StubCashFlowRepository(seedDefaultBanks: true);
        _clock = TestClock.At();
        _tracer = new RecordingTelemetryTracer();
        _logger = new RecordingLogger<TransferService>();
        _sut = CreateService();
    }

    private TransferService CreateService(StubCashFlowRepository? repository = null) =>
        new(repository ?? _repository, _clock, _tracer, _logger);

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

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Source and destination must be different accounts.");
    }

    [Fact]
    public async Task AddTransferAsync_DatedToday_Saves()
    {
        var result = await _sut.AddTransferAsync(ToCreateDto(_repository, ValidCreateRequest() with { Date = new DateOnly(2026, 10, 5) }));

        result.Date.Should().Be(new DateOnly(2026, 10, 5));
    }

    [Fact]
    public async Task AddTransferAsync_DatedTomorrow_RejectedAndNothingSaved()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { Date = new DateOnly(2026, 10, 6) });

        var act = async () => await _sut.AddTransferAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Date must not be in the future.");
        ShouldHaveRejectedAdd();
    }

    [Fact]
    [Trait("AC", "P60-F03-generic-account-to-account-transfer-08")]
    public async Task AddTransferAsync_PairNotAllowed_RejectedWithSameMessage()
    {
        _repository.Accounts.Add(
            Account.CreateWithCapabilities("Receiver", Currency.GBP, new HashSet<Capability> { Capability.CanReceiveTransfers }));
        var request = ToCreateDto(_repository, ValidCreateRequest() with { SourceBank = "Receiver", DestinationBank = "Barclays" });

        var act = async () => await _sut.AddTransferAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Transfers from Receiver to Barclays are not allowed.");
        ShouldHaveRejectedAdd();
    }

    [Fact]
    public async Task AddTransferAsync_InactiveDestination_RejectedAndNothingSaved()
    {
        _repository.Accounts.Add(Account.CreateWithCapabilities(
            "Closed", Currency.GBP, AccountTemplates.CapabilitiesOf(AccountTemplate.Bank), isActive: false));
        var request = ToCreateDto(_repository, ValidCreateRequest() with { DestinationBank = "Closed" });

        var act = async () => await _sut.AddTransferAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Closed is inactive.");
        ShouldHaveRejectedAdd();
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
        _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.TransferService.AddTransfer");
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
    public async Task UpdateTransferAsync_AccountsUnchanged_KeepsStoredDestinationAmount()
    {
        var transfer = SeedCrossCurrencyTransfer();
        var request = ToUpdateDto(_repository, ValidCreateRequest() with { DestinationBank = "Itaú", Amount = 120m, Note = "Corrected" });

        await _sut.UpdateTransferAsync(transfer.Id, request);

        using (new AssertionScope())
        {
            transfer.Amount.Should().Be(120m);
            transfer.DestinationAmount.Should().Be(640.50m);
            transfer.Note.Should().Be("Corrected");
        }
    }

    [Fact]
    public async Task UpdateTransferAsync_AccountChanged_PassesNone()
    {
        var transfer = SeedCrossCurrencyTransfer();
        var request = ToUpdateDto(_repository, ValidCreateRequest() with { DestinationBank = "Chase", Amount = 100m });

        await _sut.UpdateTransferAsync(transfer.Id, request);

        using (new AssertionScope())
        {
            transfer.DestinationAccount.Name.Should().Be("Chase");
            transfer.DestinationAmount.Should().BeNull();
        }
    }

    [Fact]
    public async Task UpdateTransferAsync_CrossCurrencyAccountChanged_RejectsMissingDestinationAmount()
    {
        var transfer = SeedCrossCurrencyTransfer();
        var request = ToUpdateDto(_repository, ValidCreateRequest() with { SourceBank = "Chase", DestinationBank = "Itaú", Amount = 100m });

        var act = async () => await _sut.UpdateTransferAsync(transfer.Id, request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Enter the amount received in BRL.");
        using (new AssertionScope())
        {
            transfer.SourceAccount.Name.Should().Be("Barclays");
            transfer.DestinationAmount.Should().Be(640.50m);
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.TransferService.UpdateTransfer");
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

        var result = _sut.GetTransfersByBank(_repository.Accounts.First(b => b.Name == "Barclays").Id);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void GetTransfersByBank_WithUnrecognizedBank_ReturnsEmptyList()
    {
        var result = _sut.GetTransfersByBank(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    private void ShouldHaveRejectedAdd()
    {
        using (new AssertionScope())
        {
            _repository.Transfers.Should().BeEmpty();
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.TransferService.AddTransfer");
            _logger.Entries.Should().NotContain(entry => entry.Exception != null || entry.Level >= LogLevel.Warning);
        }
    }

    private Transfer SeedCrossCurrencyTransfer()
    {
        var itau = TestAccounts.Bank("Itaú", currency: Currency.BRL);
        _repository.Accounts.Add(itau);
        var barclays = _repository.Accounts.First(b => b.Name == "Barclays");
        var transfer = Transfer.Create(new DateOnly(2026, 7, 25), barclays, itau, 100m, destinationAmount: 640.50m, note: null, TestClock.Today);
        _repository.Transfers.Add(transfer);
        return transfer;
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
        repository.Accounts.FirstOrDefault(b => b.Name == bankName)?.Id ?? Guid.NewGuid();

    private sealed record TransferCreateRequest(
        DateOnly Date, string SourceBank, string DestinationBank, decimal Amount, string? Note);

    [Fact]
    public void GetTransfersByMonth_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnNextRead = new InvalidOperationException("simulated failure");

        Action act = () => _sut.GetTransfersByMonth(2026, 7);

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.TransferService.GetTransfersByMonth");
    }

    [Fact]
    public void GetTransfersByBank_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnNextRead = new InvalidOperationException("simulated failure");

        Action act = () => _sut.GetTransfersByBank(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.TransferService.GetTransfersByBank");
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-10")]
    public async Task GetTransfersByMonth_LeavesOutStatementPayments()
    {
        SeedStatementPayment(new DateOnly(2026, 7, 12));
        await _sut.AddTransferAsync(ToCreateDto(_repository, ValidCreateRequest() with { Date = new DateOnly(2026, 7, 10) }));

        var result = _sut.GetTransfersByMonth(2026, 7);

        result.Should().ContainSingle().Which.DestinationBankName.Should().Be("Trading212");
    }

    [Fact]
    public void GetTransfersByBank_LeavesOutStatementPayments()
    {
        SeedStatementPayment(new DateOnly(2026, 7, 12));

        var result = _sut.GetTransfersByBank(_repository.Accounts.First(b => b.Name == "Barclays").Id);

        result.Should().BeEmpty();
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-10")]
    public async Task UpdateTransferAsync_StatementPayment_ThrowsConflictAndChangesNothing()
    {
        var payment = SeedStatementPayment(new DateOnly(2026, 7, 12));

        var act = async () => await _sut.UpdateTransferAsync(payment.Id, ToUpdateDto(_repository, ValidCreateRequest()));

        await act.Should().ThrowAsync<StatementPaymentConflictException>()
            .WithMessage("This transfer pays a card statement; unmark the statement instead.");
        using (new AssertionScope())
        {
            payment.Amount.Should().Be(40m);
            payment.Date.Should().Be(new DateOnly(2026, 7, 12));
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<StatementPaymentConflictException>("CashFlow.TransferService.UpdateTransfer");
        }
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-10")]
    public async Task DeleteTransferAsync_StatementPayment_ThrowsConflictAndKeepsIt()
    {
        var payment = SeedStatementPayment(new DateOnly(2026, 7, 12));

        var act = async () => await _sut.DeleteTransferAsync(payment.Id);

        await act.Should().ThrowAsync<StatementPaymentConflictException>()
            .WithMessage("This transfer pays a card statement; unmark the statement instead.");
        using (new AssertionScope())
        {
            _repository.Transfers.Should().ContainSingle().Which.Should().BeSameAs(payment);
            _repository.SaveChangesCallCount.Should().Be(0);
            _tracer.ShouldHaveFailedSpan<StatementPaymentConflictException>("CashFlow.TransferService.DeleteTransfer");
        }
    }

    private Transfer SeedStatementPayment(DateOnly date) =>
        TestTransfers.SeedStatementPayment(_repository, _repository.Accounts.First(a => a.Name == "Barclays"), date);
}
