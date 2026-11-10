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
    private const string AccountsPath = "/api/v1/financial/accounts";

    private readonly ApiTestFactory _baseFactory = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ThrowingAccountQueryService _accounts = new();
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
                services.RemoveAll<IAccountQueryService>();
                services.AddSingleton<IAccountQueryService>(_accounts);
            });
        });
        _client = _factory.CreateClient();
    }

    public ThrowingApiHost() : this(null)
    {
    }

    public async Task<HttpResponseMessage> GetAccountsFailingWith(Exception failure)
    {
        _accounts.Failure = failure;
        return await _client.GetAsync(AccountsPath);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _baseFactory.Dispose();
    }

    private sealed class ThrowingAccountQueryService : IAccountQueryService
    {
        public Exception Failure { get; set; } = new InvalidOperationException();

        public IReadOnlyList<AccountDTO> GetAccounts() => throw Failure;

        public AccountDetailDTO GetAccount(Guid id, int year, int month) => throw new NotSupportedException();

        public IReadOnlyList<AccountTransactionDTO> GetTransactions(Guid id, int year, int month) => throw new NotSupportedException();

        public IReadOnlyList<MonthTransactionDTO> GetMonthTransactions(int year, int month) => throw new NotSupportedException();

        public IReadOnlyList<AccountStatementDTO> GetStatements(Guid id) => throw new NotSupportedException();

        public IReadOnlyList<PayableStatementDTO> GetPayableStatements() => throw new NotSupportedException();
    }
}

public sealed class ProductionThrowingApiHost : ThrowingApiHost
{
    public ProductionThrowingApiHost() : base("Production")
    {
    }
}
