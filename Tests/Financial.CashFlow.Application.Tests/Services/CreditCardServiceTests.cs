using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Application.Tests.TestHelpers;
using Financial.CashFlow.Domain.Entities;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Services;

public class CreditCardServiceTests
{
    [Fact]
    public void GetCreditCards_ReturnsAllSeededCards_IncludingInactive()
    {
        var repository = new StubCashFlowRepository();
        repository.CreditCards.Add(CreditCard.Create("BaAmex", isActive: true));
        repository.CreditCards.Add(CreditCard.Create("PaypalCredit", isActive: false));
        var service = new CreditCardService(repository);

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
        var service = new CreditCardService(repository);
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
        var service = new CreditCardService(repository);

        var act = async () => await service.UpdateCreditCardAsync(Guid.NewGuid(), new CreditCardUpdateDTO
        {
            NextInvoiceDueDate = null,
            IsActive = true
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
        repository.SaveChangesCallCount.Should().Be(0);
    }
}
