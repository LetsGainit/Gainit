# GainIt

Collaborative platform connecting Gainers (developers), Mentors, and Nonprofits to build meaningful software. Backend is an ASP.NET Core (.NET 8) Web API with GitHub integration, task management, and realtime notifications.

## Overview
- API layer in `backend/GainIt/GainIt.API`
- Business logic via `Services/*`
- Data access with EF Core via `Data/GainItDbContext.cs`
- Domain entities in `Models/*`
- Contracts in `DTOs/*`

Full documentation lives in [Documentation](./Documentation/README.md).

## Tech Stack
- .NET 8, ASP.NET Core Web API
- Entity Framework Core (PostgreSQL via Npgsql), EF Core Migrations
- SignalR, Serilog
- OpenAPI/Swagger (Swashbuckle)
- Authentication/Authorization: Microsoft Identity Platform (JWT Bearer), authorization policies
- Logging/Monitoring: Serilog (console/file) and structured logging; Application Insights (optional)
- Health checks: ASP.NET Core HealthChecks (DB, SignalR)
- Configuration: Options pattern (`AzureStorage`, `AzureSearch`, `OpenAI`, `JoinRequest`)
- Integrations: GitHub REST API, Azure Communication Services, Azure Blob Storage

## Setup
- 1) Prereqs: .NET 8 SDK, PostgreSQL
- 2) Restore tools and packages:
  - `dotnet restore`
  - If needed: `dotnet tool install --global dotnet-ef`
- 3) Trust HTTPS dev cert (first time on this machine):
  - `dotnet dev-certs https --trust`
- 4) Configure environment (before migrate/run):
  - `ASPNETCORE_ENVIRONMENT=Development`
  - `ConnectionStrings__Default=Host=<host>;Database=<db>;Username=<user>;Password=<pass>`
  - Optional (only if used in your flows):
    - `AzureStorage__ConnectionString=...`
    - `AzureSearch__Endpoint=...`, `AzureSearch__ApiKey=...`
    - `OpenAI__ApiKey=...`
    - `JoinRequest__...`
- 5) Apply DB:
  - `cd backend/GainIt/GainIt.API && dotnet ef database update`
- 6) Run API:
  - `dotnet run`
- 7) Swagger:
  - Default: `https://localhost:7001/swagger` (verify actual port in `backend/GainIt/GainIt.API/Properties/launchSettings.json`)

## Run
- Local: `cd backend/GainIt/GainIt.API && dotnet run`
- Production: see CI/CD and appsettings; configure env vars and connection strings

## Contributing
- Branch: feature/*, fix/*
- PRs with description and screenshots for API changes
- Follow coding standards and add XML docs where relevant

## Links
- Backend docs: [Documentation](./Documentation/README.md)
