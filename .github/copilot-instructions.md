# GainIt Platform - GitHub Copilot Instructions

**ALWAYS follow these instructions first and only fallback to additional search and context gathering if the information in these instructions is incomplete or found to be in error.**

## Working Effectively

### Bootstrap, Build, and Test the Repository
- Install .NET 8 SDK if not present: Check with `dotnet --version` (should return 8.0.x)
- Install Entity Framework CLI tools: `dotnet tool install --global dotnet-ef`
- Navigate to solution directory: `cd backend/GainIt`
- Restore dependencies: `dotnet restore` -- takes ~20 seconds
- Build the solution: `dotnet build` -- takes ~15 seconds, produces warnings but no errors
- **NEVER CANCEL** builds or long-running commands. Set timeout to 120+ seconds for builds.

### Database Operations
- List migrations: `dotnet ef migrations list --project GainIt.API` -- takes ~10 seconds
- Create migration: `dotnet ef migrations add MigrationName --project GainIt.API`
- Update database: `dotnet ef database update --project GainIt.API`
- **NOTE**: Database operations require a valid PostgreSQL connection string in appsettings.json

### Running the Application
- Navigate to API directory: `cd backend/GainIt/GainIt.API`
- Run the application: `dotnet run` -- starts on HTTPS 7149, HTTP 5172
- Access Swagger UI: `https://localhost:7149/swagger` or `http://localhost:5172/swagger`
- **CRITICAL**: Application requires SignalR connection string to start successfully
- **WARNING**: Application fails to start with `AzureSignalRConfigurationNoEndpointException` without proper configuration

### Code Quality and Formatting
- Format code: `dotnet format` -- takes ~17 seconds. **NEVER CANCEL** - set timeout to 60+ seconds
- Verify formatting: `dotnet format --verify-no-changes` -- takes ~18 seconds
- **ALWAYS** run `dotnet format` before committing changes or CI will fail

## Configuration Requirements

### Required Configuration (appsettings.json)
```json
{
  "ConnectionStrings": {
    "GainItPostgresDb": "Your PostgreSQL connection string"
  },
  "SignalR": {
    "ConnectionString": "Required Azure SignalR connection string"
  },
  "GitHub": {
    "Token": "GitHub personal access token (for GitHub integration features)"
  }
}
```

### Optional but Recommended Configuration
- Application Insights connection string
- Azure B2C authentication settings
- Azure OpenAI endpoints and API keys
- Azure Communication Services for email

### Environment Variables
- `GITHUB_TOKEN`: Alternative to appsettings GitHub token
- `ASPNETCORE_ENVIRONMENT`: Set to Development for local testing

## Validation Scenarios

### Basic Validation After Changes
1. **Build Validation**: Run `dotnet build` and ensure no NEW errors are introduced
2. **Format Validation**: Run `dotnet format --verify-no-changes` and fix any issues
3. **Migration Validation**: Run `dotnet ef migrations list --project GainIt.API` to ensure EF tools work

### Manual Application Testing
1. **Startup Test**: Attempt to run `dotnet run` and verify it attempts to start (will fail at SignalR without config)
2. **Swagger Access**: If properly configured, access Swagger UI at the application URL
3. **GitHub Integration Test**: Test GitHub repository linking via `/api/github/link` endpoint
4. **Database Test**: Test any database operations if properly configured

## Critical Timing and Timeout Information

### Build and Restore Operations
- `dotnet restore`: 15-25 seconds. **NEVER CANCEL** - set timeout to 60+ seconds
- `dotnet build`: 10-18 seconds. **NEVER CANCEL** - set timeout to 60+ seconds
- `dotnet format`: 15-20 seconds. **NEVER CANCEL** - set timeout to 60+ seconds

### Database Operations
- `dotnet ef migrations list`: 8-15 seconds. **NEVER CANCEL** - set timeout to 30+ seconds
- `dotnet ef migrations add`: 10-20 seconds. **NEVER CANCEL** - set timeout to 45+ seconds
- `dotnet ef database update`: Variable (depends on migration complexity). **NEVER CANCEL** - set timeout to 120+ seconds

### Application Startup
- Application startup: 2-5 seconds to reach SignalR configuration check
- Full startup (with proper config): Expected 5-10 seconds

## Common File Locations and Navigation

### Key Directories
- **Solution Root**: `backend/GainIt/`
- **Main API Project**: `backend/GainIt/GainIt.API/`
- **Controllers**: `backend/GainIt/GainIt.API/Controllers/`
- **Models**: `backend/GainIt/GainIt.API/Models/`
- **Services**: `backend/GainIt/GainIt.API/Services/`
- **DTOs**: `backend/GainIt/GainIt.API/DTOs/`
- **Migrations**: `backend/GainIt/GainIt.API/Migrations/`
- **Documentation**: `backend/GainIt/GainIt.API/Documentation/`

### Important Files
- **Main Program**: `backend/GainIt/GainIt.API/Program.cs`
- **Project File**: `backend/GainIt/GainIt.API/GainIt.API.csproj`
- **Database Context**: `backend/GainIt/GainIt.API/Data/GainItDbContext.cs`
- **App Settings**: `backend/GainIt/GainIt.API/appsettings.json`
- **Launch Settings**: `backend/GainIt/GainIt.API/Properties/launchSettings.json`

### Architecture Patterns
- **Clean Architecture**: Services contain business logic, Controllers handle HTTP
- **Repository Pattern**: DbContext provides data access
- **Service Layer**: Located in Services/ directory with interface/implementation pattern
- **DTO Pattern**: Request/Response models in DTOs/ directory

## Technology Stack Summary

### Core Technologies
- **.NET 8 Web API**: Primary framework
- **Entity Framework Core 9.0.7**: ORM with PostgreSQL
- **PostgreSQL**: Primary database
- **Serilog**: Logging framework with console and file outputs

### Azure Integrations
- **Azure SignalR**: Real-time communication
- **Azure B2C**: Authentication and authorization
- **Azure OpenAI**: AI-powered features
- **Application Insights**: Monitoring and telemetry
- **Azure Communication Services**: Email services

### Third-Party Services
- **GitHub REST API**: Repository integration and analytics
- **Swagger/OpenAPI**: API documentation

## Development Workflow

### Adding New Features
1. Create models in appropriate `Models/` subdirectory
2. Add DTOs in `DTOs/Requests/` or `DTOs/ViewModels/`
3. Implement business logic in `Services/` with interface
4. Create controller in `Controllers/` for API endpoints
5. Add database migration if model changes: `dotnet ef migrations add FeatureName --project GainIt.API`
6. **ALWAYS** run `dotnet format` before committing
7. Test the feature manually via Swagger UI

### Code Style Guidelines
- Follow C# naming conventions (PascalCase for public members)
- Use async/await for all database operations
- Implement proper error handling with try-catch
- Add XML documentation for public APIs
- Use dependency injection for service dependencies

## Known Issues and Limitations

### Application Startup
- **Cannot run without SignalR configuration**: Application fails with `AzureSignalRConfigurationNoEndpointException`
- **Development environment**: Authentication/authorization middleware is skipped in Development
- **GitHub integration**: Requires GITHUB_TOKEN environment variable or appsettings configuration

### Build Warnings
- Multiple nullable reference warnings (CS8618, CS8601, CS8602) - these are non-blocking
- Inheritance hiding warnings in Mentor.cs - existing issue, non-blocking
- Async method warnings in GitHubAnalyticsService.cs - existing issue, non-blocking

### Performance Notes
- Performance monitoring middleware logs slow requests (>10 seconds) and high memory usage (>10MB)
- GitHub API client has 30-second timeout for external calls
- Database health checks run automatically

## Testing and Validation Commands Summary

```bash
# Build and validate (run from backend/GainIt/)
dotnet restore                           # ~20s - NEVER CANCEL
dotnet build                            # ~15s - NEVER CANCEL  
dotnet format --verify-no-changes       # ~18s - NEVER CANCEL
dotnet ef migrations list --project GainIt.API  # ~10s - NEVER CANCEL

# Format code (run from backend/GainIt/)
dotnet format                           # ~17s - NEVER CANCEL

# Run application (run from backend/GainIt/GainIt.API/)
dotnet run                              # Starts app on ports 7149/5172
```

**REMEMBER**: Always set timeouts of 60+ seconds for build operations and NEVER cancel long-running commands. This repository builds successfully but has specific configuration requirements for full functionality.