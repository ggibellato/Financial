using System;
using System.IO;
using System.Linq;
using Financial.Application.DTOs;
using Financial.Infrastructure.Persistence;
using Financial.Infrastructure.Repositories;
using FluentAssertions;

namespace Financial.Infrastructure.Tests;

public class CreditServiceTests
{
    [Fact]
    public void AddCredit_WithValidRequest_ReturnsDetailsWithNewCredit()
    {
        var (service, tempFile) = CreateService();
        try
        {
            var request = new CreditCreateDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Date = new DateTime(2024, 2, 1),
                Type = "Dividend",
                Value = 12.5m
            };

            var result = service.AddCredit(request);

            result.Should().NotBeNull();
            result!.Credits.Should().ContainSingle(credit =>
                credit.Date == request.Date &&
                credit.Type == request.Type &&
                credit.Value == request.Value &&
                credit.Id != Guid.Empty);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void UpdateCredit_WithValidRequest_UpdatesCredit()
    {
        var (service, tempFile) = CreateService();
        try
        {
            var created = service.AddCredit(new CreditCreateDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Date = new DateTime(2024, 2, 2),
                Type = "Dividend",
                Value = 5m
            });

            var creditId = created!.Credits.First(credit => credit.Date == new DateTime(2024, 2, 2)).Id;

            var updated = service.UpdateCredit(new CreditUpdateDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Id = creditId,
                Date = new DateTime(2024, 2, 2),
                Type = "Rent",
                Value = 8.75m
            });

            updated.Should().NotBeNull();
            var updatedCredit = updated!.Credits.Single(credit => credit.Id == creditId);
            updatedCredit.Type.Should().Be("Rent");
            updatedCredit.Value.Should().Be(8.75m);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void DeleteCredit_WithValidRequest_RemovesCredit()
    {
        var (service, tempFile) = CreateService();
        try
        {
            var created = service.AddCredit(new CreditCreateDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Date = new DateTime(2024, 2, 3),
                Type = "Dividend",
                Value = 4m
            });

            var creditId = created!.Credits.First(credit => credit.Date == new DateTime(2024, 2, 3)).Id;

            var updated = service.DeleteCredit(new CreditDeleteDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Id = creditId
            });

            updated.Should().NotBeNull();
            updated!.Credits.Should().NotContain(credit => credit.Id == creditId);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private static (CreditService Service, string TempFile) CreateService()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"data.test.{Guid.NewGuid():N}.json");
        File.Copy(TestDataPaths.DataJsonFile, tempFile, true);

        var repository = new JSONRepository(new LocalJsonStorage(tempFile));
        var navigationService = new NavigationService(repository);
        var service = new CreditService(repository, navigationService);

        return (service, tempFile);
    }
}
