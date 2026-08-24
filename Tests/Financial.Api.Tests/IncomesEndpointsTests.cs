using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class IncomesEndpointsTests : ApiEndpointTests
{
    private static readonly Guid BarclaysId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000001");
    private static readonly Guid Trading212Id = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000002");
    private static readonly Guid ChaseId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000003");
    private static readonly Guid GleisonId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-000000000001");
    private static readonly Guid ArianaId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-000000000002");
    private static readonly Guid LotteryId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-000000000003");
    private static readonly Guid DividendoJurosId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-000000000004");

    [Fact]
    public async Task AddIncome_ValidRequest_ReturnsOk()
    {
        var request = new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            IncomeSourceId = GleisonId,
            GrossValue = 3200.00m,
            NetValue = 2450.00m,
            BankId = BarclaysId
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/incomes", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var income = await response.Content.ReadFromJsonAsync<IncomeDTO>();
        income.Should().NotBeNull();
        income!.IncomeSourceId.Should().Be(GleisonId);
        income.IncomeSourceName.Should().Be("Gleison");
        income.GrossValue.Should().Be(3200.00m);
        income.NetValue.Should().Be(2450.00m);
        income.BankId.Should().Be(BarclaysId);
        income.BankName.Should().Be("Barclays");
    }

    [Fact]
    public async Task AddIncome_UnrecognizedBank_ReturnsBadRequest()
    {
        var request = new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            IncomeSourceId = LotteryId,
            GrossValue = null,
            NetValue = 50m,
            BankId = Guid.NewGuid()
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/incomes", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddIncome_WithoutBank_ReturnsOk()
    {
        var request = new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            IncomeSourceId = LotteryId,
            GrossValue = null,
            NetValue = 50m,
            BankId = null
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/incomes", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var income = await response.Content.ReadFromJsonAsync<IncomeDTO>();
        income!.BankId.Should().BeNull();
        income.BankName.Should().BeNull();
    }

    [Fact]
    public async Task AddIncome_WithDescription_ReturnsOkWithDescription()
    {
        var request = new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            IncomeSourceId = LotteryId,
            GrossValue = null,
            NetValue = 50m,
            BankId = null,
            Description = "Chip ISA dividend"
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/incomes", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var income = await response.Content.ReadFromJsonAsync<IncomeDTO>();
        income!.Description.Should().Be("Chip ISA dividend");
    }

    [Fact]
    public async Task AddIncome_WithDescriptionOver200Characters_ReturnsBadRequest()
    {
        var request = new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            IncomeSourceId = LotteryId,
            GrossValue = null,
            NetValue = 50m,
            BankId = ChaseId,
            Description = new string('a', 201)
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/incomes", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddIncome_NegativeNetValue_ReturnsBadRequest()
    {
        var request = new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            IncomeSourceId = LotteryId,
            GrossValue = null,
            NetValue = -1m,
            BankId = ChaseId
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/incomes", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateIncome_ExistingId_ReturnsOkAndUpdatesFields()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            IncomeSourceId = LotteryId,
            GrossValue = null,
            NetValue = 10m,
            BankId = ChaseId
        });
        var createdIncome = await created.Content.ReadFromJsonAsync<IncomeDTO>();

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/incomes/{createdIncome!.Id}", new IncomeUpdateDTO
        {
            Date = new DateOnly(2026, 7, 2),
            IncomeSourceId = DividendoJurosId,
            GrossValue = null,
            NetValue = 25m,
            BankId = Trading212Id
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<IncomeDTO>();
        updated!.IncomeSourceId.Should().Be(DividendoJurosId);
        updated.IncomeSourceName.Should().Be("DividendoJuros");
        updated.NetValue.Should().Be(25m);
        updated.BankId.Should().Be(Trading212Id);
        updated.BankName.Should().Be("Trading212");
    }

    [Fact]
    public async Task UpdateIncome_RemovingBank_ReturnsOkWithNullBank()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            IncomeSourceId = LotteryId,
            GrossValue = null,
            NetValue = 10m,
            BankId = ChaseId
        });
        var createdIncome = await created.Content.ReadFromJsonAsync<IncomeDTO>();

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/incomes/{createdIncome!.Id}", new IncomeUpdateDTO
        {
            Date = createdIncome.Date,
            IncomeSourceId = createdIncome.IncomeSourceId,
            GrossValue = null,
            NetValue = createdIncome.NetValue,
            BankId = null
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<IncomeDTO>();
        updated!.BankId.Should().BeNull();
        updated.BankName.Should().BeNull();
    }

    [Fact]
    public async Task UpdateIncome_UnknownId_ReturnsNotFound()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/incomes/{Guid.NewGuid()}", new IncomeUpdateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            IncomeSourceId = LotteryId,
            GrossValue = null,
            NetValue = 10m,
            BankId = ChaseId
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteIncome_ExistingId_ReturnsOkAndRemovesIncome()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 5),
            IncomeSourceId = LotteryId,
            GrossValue = null,
            NetValue = 10m,
            BankId = ChaseId
        });
        var createdIncome = await created.Content.ReadFromJsonAsync<IncomeDTO>();

        var response = await Client.DeleteAsync($"/api/v1/financial/incomes/{createdIncome!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await Client.GetFromJsonAsync<List<IncomeDTO>>("/api/v1/financial/incomes/month/2026/7");
        list.Should().NotContain(i => i.Id == createdIncome.Id);
    }

    [Fact]
    public async Task DeleteIncome_UnknownId_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"/api/v1/financial/incomes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetIncomesByMonth_ReturnsOnlyIncomesForThatMonthAndAllowsMultiplePerSource()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            IncomeSourceId = ArianaId,
            GrossValue = null,
            NetValue = 400m,
            BankId = ChaseId
        });
        await Client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 8),
            IncomeSourceId = ArianaId,
            GrossValue = null,
            NetValue = 420m,
            BankId = ChaseId
        });
        await Client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 8, 1),
            IncomeSourceId = ArianaId,
            GrossValue = null,
            NetValue = 410m,
            BankId = ChaseId
        });

        var response = await Client.GetAsync("/api/v1/financial/incomes/month/2026/7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<IncomeDTO>>();
        items.Should().HaveCount(2);
        items.Should().OnlyContain(i => i.Date.Month == 7);
    }

    [Fact]
    public async Task AddIncome_WithSplitForEligibleSource_ReturnsOkAndCreatesLinkedReserveMovements()
    {
        var request = new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            IncomeSourceId = ArianaId,
            GrossValue = null,
            NetValue = 2450.00m,
            BankId = ChaseId,
            SplitToReserve = true
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/incomes", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var income = await response.Content.ReadFromJsonAsync<IncomeDTO>();
        income!.SplitToReserve.Should().BeTrue();
        var movements = await Client.GetFromJsonAsync<List<ReserveMovementDTO>>("/api/v1/financial/reserve/movements");
        var linkedMovements = movements!.Where(m => m.IncomeId == income.Id).ToList();
        linkedMovements.Should().HaveCount(4);
        linkedMovements.Sum(m => m.Amount).Should().Be(2205.00m);
    }

    [Fact]
    public async Task AddIncome_WithSplitForIneligibleSource_ReturnsBadRequest()
    {
        var request = new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            IncomeSourceId = GleisonId,
            GrossValue = 3200.00m,
            NetValue = 2450.00m,
            BankId = BarclaysId,
            SplitToReserve = true
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/incomes", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteIncome_WithLinkedMovements_RemovesThemFromMovementHistory()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            IncomeSourceId = ArianaId,
            GrossValue = null,
            NetValue = 2450.00m,
            BankId = ChaseId,
            SplitToReserve = true
        });
        var createdIncome = await created.Content.ReadFromJsonAsync<IncomeDTO>();
        var movementsBeforeDelete = await Client.GetFromJsonAsync<List<ReserveMovementDTO>>("/api/v1/financial/reserve/movements");
        movementsBeforeDelete!.Should().Contain(m => m.IncomeId == createdIncome!.Id);

        var response = await Client.DeleteAsync($"/api/v1/financial/incomes/{createdIncome!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var movementsAfterDelete = await Client.GetFromJsonAsync<List<ReserveMovementDTO>>("/api/v1/financial/reserve/movements");
        movementsAfterDelete.Should().NotContain(m => m.IncomeId == createdIncome.Id);
    }
}
