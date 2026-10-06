using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Financial.Api.Tests;

public class ThrowingApiHost : IDisposable
{
    private const string BanksPath = "/api/v1/financial/banks";

    private readonly ApiTestFactory _baseFactory = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ThrowingBankService _banks = new();
    private readonly HttpClient _client;

    protected ThrowingApiHost(string? environment)
    {
        _factory = _baseFactory.WithWebHostBuilder(builder =>
        {
            if (environment is not null)
            {
                builder.UseEnvironment(environment);
            }

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IBankService>();
                services.AddSingleton<IBankService>(_banks);
            });
        });
        _client = _factory.CreateClient();
    }

    public ThrowingApiHost() : this(null)
    {
    }

    public async Task<HttpResponseMessage> GetBanksFailingWith(Exception failure)
    {
        _banks.Failure = failure;
        return await _client.GetAsync(BanksPath);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _baseFactory.Dispose();
    }

    private sealed class ThrowingBankService : IBankService
    {
        public Exception Failure { get; set; } = new InvalidOperationException();

        public IReadOnlyList<BankDTO> GetBanks() => throw Failure;

        public Task<BankDTO> CreateBankAsync(BankCreateDTO request) => throw new NotSupportedException();

        public Task<BankDTO> UpdateBankAsync(Guid id, BankUpdateDTO request) => throw new NotSupportedException();

        public Task DeleteBankAsync(Guid id) => throw new NotSupportedException();

        public Task<BankDTO> UpdateOpeningBalanceAsync(Guid id, BankOpeningBalanceUpdateDTO request) => throw new NotSupportedException();

        public IReadOnlyList<BankBalanceDTO> GetBankBalancesByMonth(int year, int month) => throw new NotSupportedException();

        public decimal GetBankBalanceAsOf(Guid bankId, DateOnly asOfDate, Guid? excludingAdjustmentId = null) => throw new NotSupportedException();
    }
}

public sealed class ProductionThrowingApiHost : ThrowingApiHost
{
    public ProductionThrowingApiHost() : base("Production")
    {
    }
}
