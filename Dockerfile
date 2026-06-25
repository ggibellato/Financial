# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Financial.slnx .
COPY Financial.Api/Financial.Api.csproj Financial.Api/
COPY Financial.Application/Financial.Application.csproj Financial.Application/
COPY Financial.Domain/Financial.Domain.csproj Financial.Domain/
COPY Financial.Infrastructure/Financial.Infrastructure.csproj Financial.Infrastructure/
COPY Integrations/FinancialToolSupport/FinancialToolSupport.csproj Integrations/FinancialToolSupport/
COPY Integrations/GoogleFinancialSupport/GoogleFinancialSupport.csproj Integrations/GoogleFinancialSupport/
COPY Integrations/WebPageParser/WebPageParser.csproj Integrations/WebPageParser/

RUN dotnet restore Financial.Api/Financial.Api.csproj

COPY Financial.Api/ Financial.Api/
COPY Financial.Application/ Financial.Application/
COPY Financial.Domain/ Financial.Domain/
COPY Financial.Infrastructure/ Financial.Infrastructure/
COPY Integrations/FinancialToolSupport/ Integrations/FinancialToolSupport/
COPY Integrations/GoogleFinancialSupport/ Integrations/GoogleFinancialSupport/
COPY Integrations/WebPageParser/ Integrations/WebPageParser/

RUN dotnet publish Financial.Api/Financial.Api.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Financial.Api.dll"]
