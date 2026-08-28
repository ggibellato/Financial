using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class CreditCardServiceTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger<CreditCardService> Logger = NullLogger<CreditCardService>.Instance;

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly CreditCardService _sut;

    public CreditCardServiceTests()
    {
        _repository = new StubCashFlowRepository();
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    private CreditCardService CreateService(StubCashFlowRepository? repository = null) =>
        new(repository ?? _repository, _tracer, Logger);

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new CreditCardService(null!, _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new CreditCardService(_repository, null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void GetCreditCards_ReturnsAllSeededCards_IncludingInactive()
    {
        _repository.CreditCards.Add(CreditCard.Create("BaAmex", isActive: true));
        _repository.CreditCards.Add(CreditCard.Create("PaypalCredit", isActive: false));

        var result = _sut.GetCreditCards();

        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Name == "PaypalCredit" && !c.IsActive);
    }

    [Fact]
    public async Task UpdateCreditCardAsync_ExistingId_ReturnsUpdatedDtoAndPersists()
    {
        var card = CreditCard.Create("BaAmex", isActive: true);
        _repository.CreditCards.Add(card);
        var dueDate = new DateOnly(2026, 9, 5);

        var result = await _sut.UpdateCreditCardAsync(card.Id, new CreditCardUpdateDTO
        {
            NextInvoiceDueDate = dueDate,
            IsActive = false
        });

        result.NextInvoiceDueDate.Should().Be(dueDate);
        result.IsActive.Should().BeFalse();
        _repository.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateCreditCardAsync_UnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.UpdateCreditCardAsync(Guid.NewGuid(), new CreditCardUpdateDTO
        {
            NextInvoiceDueDate = null,
            IsActive = true
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repository.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new CreditCardService(_repository, _tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
