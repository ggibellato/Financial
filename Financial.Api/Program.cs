using Financial.Application.Interfaces;
using Financial.Infrastructure.Persistence;
using Financial.Infrastructure.Repositories;
using Financial.Infrastructure.Services;
using System;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddControllers();

const string RepositoryProviderConfigurationKey = "Repository:Provider";
const string CorsOriginsConfigurationKey = "Cors:AllowedOrigins";

var allowedOrigins = configuration.GetSection(CorsOriginsConfigurationKey).Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            policy.SetIsOriginAllowed(_ => false);
        }
    });
});

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

    var dataJsonFile = configuration[LocalJsonStorage.DataJsonFileConfigurationKey];
    var credentialsPath = configuration[GoogleDriveJsonStorage.CredentialsPathConfigurationKey];

    var options = new RepositorySelectionOptions(
        provider,
        dataJsonFile,
        credentialsPath,
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
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler();
}

app.UseHttpsRedirection();
app.UseCors();

var api = app.MapGroup("/api/v1/financial");
api.MapControllers();

app.Run();

public partial class Program
{
}
