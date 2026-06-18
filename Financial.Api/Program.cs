using Financial.Application.Configuration;
using Financial.Application.DependencyInjection;
using Financial.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddControllers();

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
builder.Services.AddFinancialApplication();
builder.Services.AddFinancialInfrastructure(configuration);

var app = builder.Build();

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
