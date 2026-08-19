using Asp.Versioning;
using Financial.Api.Middleware;
using Financial.CashFlow.Application.DependencyInjection;
using Financial.CashFlow.Infrastructure.DependencyInjection;
using Financial.Investment.Application.Configuration;
using Financial.Investment.Application.DependencyInjection;
using Financial.Investment.Infrastructure.DependencyInjection;
using Financial.Investment.Infrastructure.Integrations.GoogleFinancialSupport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.File(
            Path.Combine(AppContext.BaseDirectory, "logs", "app-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14);
});

var defaultApiVersion = new ApiVersion(1, 0);
const string ApiRouteSegment = "financial";
var apiVersionGroupName = $"v{defaultApiVersion.MajorVersion}";
var apiRoutePrefix = $"/api/{apiVersionGroupName}/{ApiRouteSegment}";

builder.Services.AddProblemDetails();

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = defaultApiVersion;
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
     {
         options.GroupNameFormat = "'v'VVV";
         options.SubstituteApiVersionInUrl = true;
     });

builder.Services
    .AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Servers = [new OpenApiServer { Url = apiRoutePrefix }];
            return Task.CompletedTask;
        });
    })
    .AddControllers(options => options.Filters.Add(new ProducesAttribute("application/json")));

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

builder.Services.Configure<DividendOptions>(configuration.GetSection(DividendOptions.SectionName));
builder.Services.Configure<WatchlistOptions>(configuration.GetSection(WatchlistOptions.SectionName));
builder.Services.Configure<AssetPriceFetchOptions>(configuration.GetSection(AssetPriceFetchOptions.SectionName));
builder.Services.AddFinancialApplication();
builder.Services.AddGoogleDriveFileClient();
builder.Services.AddFinancialInfrastructure(configuration);
builder.Services.AddFinancialCashFlowApplication();
builder.Services.AddFinancialCashFlowInfrastructure(configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint($"/openapi/{apiVersionGroupName}.json", $"Financial API {apiVersionGroupName}");
        options.RoutePrefix = "swagger";
    });
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler();
}

app.UseMiddleware<DomainExceptionMappingMiddleware>();

app.UseCors();
app.UseStaticFiles();

var api = app.MapGroup($"/api/v{{version:apiVersion}}/{ApiRouteSegment}");
api.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();

public partial class Program
{
}
