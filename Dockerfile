# Stage 1: Build React frontend
FROM node:22-slim AS web-build
WORKDIR /app
COPY Financial.Web/package*.json .
RUN npm install
COPY Financial.Web/ .
RUN echo "API_BASE_URL=" > .env
RUN npm run build

# Stage 2: Build .NET API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
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

# Stage 3: Runtime — API serves both the REST endpoints and the React SPA
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=api-build /app/publish .
COPY --from=web-build /app/dist ./wwwroot/
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Financial.Api.dll"]
