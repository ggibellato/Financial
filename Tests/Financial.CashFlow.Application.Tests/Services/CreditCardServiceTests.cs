using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Services;

public class CreditCardServiceTests
{
    private static readonly ITelemetryTracer Tracer = new RecordingTelemetryTracer();

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new CreditCardService(null!, Tracer);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new CreditCardService(new StubCashFlowRepository(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void GetCreditCards_ReturnsAllSeededCards_IncludingInactive()
    {
        var repository = new StubCashFlowRepository();
        repository.CreditCards.Add(CreditCard.Create("BaAmex", isActive: true));
        repository.CreditCards.Add(CreditCard.Create("PaypalCredit", isActive: false));
        var service = new CreditCardService(repository, Tracer);

        var result = service.GetCreditCards();

        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Name == "PaypalCredit" && !c.IsActive);
    }

    [Fact]
    public async Task UpdateCreditCardAsync_ExistingId_ReturnsUpdatedDtoAndPersists()
    {
        var repository = new StubCashFlowRepository();
        var card = CreditCard.Create("BaAmex", isActive: true);
        repository.CreditCards.Add(card);
        var service = new CreditCardService(repository, Tracer);
        var dueDate = new DateOnly(2026, 9, 5);

        var result = await service.UpdateCreditCardAsync(card.Id, new CreditCardUpdateDTO
        {
            NextInvoiceDueDate = dueDate,
            IsActive = false
        });

        result.NextInvoiceDueDate.Should().Be(dueDate);
        result.IsActive.Should().BeFalse();
        repository.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateCreditCardAsync_UnknownId_ThrowsKeyNotFoundException()
    {
        var repository = new StubCashFlowRepository();
        var service = new CreditCardService(repository, Tracer);

        var act = async () => await service.UpdateCreditCardAsync(Guid.NewGuid(), new CreditCardUpdateDTO
        {
            NextInvoiceDueDate = null,
            IsActive = true
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
        repository.SaveChangesCallCount.Should().Be(0);
    }
}
