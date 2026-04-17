using Financial.Application.Interfaces;
using Financial.Infrastructure.Persistence;
using Financial.Infrastructure.Repositories;
using Financial.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

const string RepositoryProviderConfigurationKey = "Repository:Provider";

builder.Services.AddSingleton<IRepositoryFactory, RepositoryFactory>();
builder.Services.AddSingleton<IRepository>(sp =>
{
    var providerValue = configuration[RepositoryProviderConfigurationKey]
        ?? nameof(RepositoryProvider.LocalJson);
    if (!Enum.TryParse(providerValue, true, out RepositoryProvider provider))
    {
        throw new InvalidOperationException(
            $"Repository provider '{providerValue}' is not supported. " +
            $"Valid values: {string.Join(", ", Enum.GetNames<RepositoryProvider>())}.");
    }

    var options = new RepositorySelectionOptions(
        provider,
        configuration[LocalJsonStorage.DataJsonFileConfigurationKey],
        configuration[GoogleDriveJsonStorage.CredentialsPathConfigurationKey],
        configuration[GoogleDriveJsonStorage.FilePathConfigurationKey]);

    var factory = sp.GetRequiredService<IRepositoryFactory>();
    return factory.Create(options);
});
builder.Services.AddSingleton<INavigationService, NavigationService>();
builder.Services.AddSingleton<IOperationService, OperationService>();
builder.Services.AddSingleton<ICreditService, CreditService>();
builder.Services.AddSingleton<IAssetPriceService, AssetPriceService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var api = app.MapGroup("/api/v1/financial");
api.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("Health");
api.MapControllers();

app.Run();
