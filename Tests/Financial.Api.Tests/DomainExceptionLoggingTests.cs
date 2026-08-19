using Financial.Api.Middleware;
using Financial.CashFlow.Application.Exceptions;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Financial.Api.Tests;

/// <summary>Covers DomainExceptionMappingMiddleware's logging: every domain exception it
/// translates into a 4xx must leave a record in the log stream, but that record must never
/// carry the exception message - those embed financial values and entity names.</summary>
public class DomainExceptionLoggingTests
{
    [Fact]
    public async Task RejectedWithdrawal_LogsTheExceptionType_WithoutTheFinancialValuesInItsMessage()
    {
        // The real message ReserveService throws, verbatim in shape: it names the bucket and its balance.
        var exception = new OverdraftConfirmationRequiredException(
            "This withdrawal exceeds Ariana's balance of 654.27. Set confirmed=true to proceed.");
        var (logger, context) = await InvokeWithAsync(exception, "POST", "/api/v1/financial/reserve/withdrawals");

        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);

        var entry = logger.Entries.Should().ContainSingle().Which;
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Message.Should().Contain(nameof(OverdraftConfirmationRequiredException));
        entry.Message.Should().Contain("409");
        entry.Message.Should().Contain("/api/v1/financial/reserve/withdrawals");

        // The bucket name and its balance must not leak into the log stream.
        entry.Message.Should().NotContain("Ariana");
        entry.Message.Should().NotContain("654.27");
    }

    [Fact]
    public async Task RejectedWithdrawal_StillWritesTheFullMessageToTheCaller()
    {
        // The redaction is log-side only - the API response must keep explaining why it failed.
        var exception = new OverdraftConfirmationRequiredException(
            "This withdrawal exceeds Ariana's balance of 654.27. Set confirmed=true to proceed.");

        var (_, context) = await InvokeWithAsync(exception, "POST", "/api/v1/financial/reserve/withdrawals");

        var body = await ReadResponseBodyAsync(context);
        body.Should().Contain("Ariana");
    }

    [Fact]
    public async Task NotFoundEntity_LogsTheExceptionTypeAsAWarning()
    {
        var exception = new KeyNotFoundException("Expense was not found.");

        var (logger, context) = await InvokeWithAsync(exception, "DELETE", "/api/v1/financial/expenses/x");

        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var entry = logger.Entries.Should().ContainSingle().Which;
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Message.Should().Contain(nameof(KeyNotFoundException));
        entry.Message.Should().Contain("404");
    }

    [Fact]
    public async Task InvalidArgument_LogsTheExceptionTypeAsAWarning()
    {
        var exception = new ArgumentException("Value must be greater than zero.");

        var (logger, context) = await InvokeWithAsync(exception, "POST", "/api/v1/financial/expenses");

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var entry = logger.Entries.Should().ContainSingle().Which;
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Message.Should().Contain(nameof(ArgumentException));
        entry.Message.Should().Contain("400");
    }

    [Fact]
    public async Task SuccessfulRequest_LogsNothing()
    {
        var logger = new RecordingLogger<DomainExceptionMappingMiddleware>();
        var context = CreateHttpContext("GET", "/api/v1/financial/banks");
        var middleware = new DomainExceptionMappingMiddleware(_ => Task.CompletedTask, logger);

        await middleware.InvokeAsync(context);

        logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task UnmappedException_IsNotSwallowedOrLogged()
    {
        // Only the three mapped domain exception types are translated; anything else must keep
        // propagating to the outer exception handler rather than being quietly logged here.
        var logger = new RecordingLogger<DomainExceptionMappingMiddleware>();
        var context = CreateHttpContext("GET", "/api/v1/financial/banks");
        var middleware = new DomainExceptionMappingMiddleware(
            _ => throw new InvalidOperationException("boom"), logger);

        var act = async () => await middleware.InvokeAsync(context);

        await act.Should().ThrowAsync<InvalidOperationException>();
        logger.Entries.Should().BeEmpty();
    }

    private static async Task<(RecordingLogger<DomainExceptionMappingMiddleware> Logger, HttpContext Context)> InvokeWithAsync(
        Exception exception,
        string method,
        string path)
    {
        var logger = new RecordingLogger<DomainExceptionMappingMiddleware>();
        var context = CreateHttpContext(method, path);
        var middleware = new DomainExceptionMappingMiddleware(_ => throw exception, logger);

        await middleware.InvokeAsync(context);

        return (logger, context);
    }

    private static HttpContext CreateHttpContext(string method, string path)
    {
        var services = new ServiceCollection();
        services.AddProblemDetails();
        services.AddLogging();

        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        return context;
    }

    private static async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
