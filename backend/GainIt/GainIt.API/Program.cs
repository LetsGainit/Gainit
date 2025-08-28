using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using GainIt.API.Data;
using GainIt.API.HealthChecks;
using GainIt.API.Middleware;
using GainIt.API.Options;
using GainIt.API.Realtime;
using GainIt.API.Services.Email.Implementations;
using GainIt.API.Services.Email.Interfaces;
using GainIt.API.Services.GitHub.Implementations;
using GainIt.API.Services.GitHub.Interfaces;
using GainIt.API.Services.Projects.Implementations;
using GainIt.API.Services.Projects.Interfaces;
using GainIt.API.Services.Tasks.Implementations;
using GainIt.API.Services.Tasks.Interfaces;
using GainIt.API.Services.Users.Implementations;
using GainIt.API.Services.Users.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

// Build configuration first
var configBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.GitHub.json", optional: true, reloadOnChange: true) // Base GitHub config (empty values for production)
    .AddJsonFile("appsettings.GitHub.Development.json", optional: true, reloadOnChange: true) // Development GitHub config (local dev only)
    .AddEnvironmentVariables(); // Production: GitHub secrets from Azure Web App environment variables

var configuration = configBuilder.Build();

// Configure Serilog with Application Insights
var loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration);

// Add Application Insights sink if instrumentation key is available
var instrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");
var connectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");

// Add GitHub environment variables (same pattern as Application Insights)
var githubAppId = Environment.GetEnvironmentVariable("GITHUB__APPID");
var githubClientId = Environment.GetEnvironmentVariable("GITHUB__CLIENTID");
var githubClientSecret = Environment.GetEnvironmentVariable("GITHUB__CLIENTSECRET");
var githubPrivateKey = Environment.GetEnvironmentVariable("GITHUB__PRIVATEKEYCONTENT");
var githubInstallationId = Environment.GetEnvironmentVariable("GITHUB__INSTALLATIONID");



if (!string.IsNullOrWhiteSpace(connectionString))
{
    // Use the full connection string if available
    var cleanConnectionString = connectionString.Trim().Replace("\"", "").Replace("'", "");
    
    loggerConfig.WriteTo.ApplicationInsights(
        cleanConnectionString,
        new Serilog.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter());
    
    Log.Information("Application Insights sink added using full connection string");
    Log.Information($"Connection String: {cleanConnectionString.Substring(0, Math.Min(50, cleanConnectionString.Length))}...");
}
else if (!string.IsNullOrWhiteSpace(instrumentationKey))
{
    // Fallback to instrumentation key only
    var cleanInstrumentationKey = instrumentationKey.Trim().Replace("\"", "").Replace("'", "");
    var appInsightsConnectionString = $"InstrumentationKey={cleanInstrumentationKey}";
    
    loggerConfig.WriteTo.ApplicationInsights(
        appInsightsConnectionString,
        new Serilog.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter());
    
    Log.Information("Application Insights sink added using instrumentation key only");
    Log.Information($"Instrumentation Key: {cleanInstrumentationKey.Substring(0, Math.Min(10, cleanInstrumentationKey.Length))}...");
}
else
{
    Log.Warning("No Application Insights environment variables found");
}

// Create the logger only once after all configuration is complete
Log.Logger = loggerConfig.CreateLogger();



// Test the Application Insights sink immediately if it was configured
if (!string.IsNullOrWhiteSpace(connectionString) || !string.IsNullOrWhiteSpace(instrumentationKey))
{
    Log.Information("=== IMMEDIATE TEST: Application Insights sink test ===");
    Log.Warning("=== IMMEDIATE TEST: This warning should appear in Application Insights ===");
    Log.Error("=== IMMEDIATE TEST: This error should appear in Application Insights ===");
}

try
{
    Log.Information("Starting GainIt.API application...");

    var builder = WebApplication.CreateBuilder(args);

    // Clear default logging providers to prevent duplicate logs
    builder.Logging.ClearProviders();

    // Ensure GitHub configuration files are included in builder.Configuration as well
    builder.Configuration
        .AddJsonFile("appsettings.GitHub.json", optional: true, reloadOnChange: true)
        .AddJsonFile("appsettings.GitHub.Development.json", optional: true, reloadOnChange: true);

    // Use the pre-configured Serilog
    builder.Host.UseSerilog();

    // Debug: Check environment variables for Application Insights (reuse variables from above)
    Log.Information("=== ENVIRONMENT VARIABLES DEBUG ===");
    Log.Information($"APPINSIGHTS_INSTRUMENTATIONKEY: {(string.IsNullOrEmpty(instrumentationKey) ? "NOT SET" : "SET")}");
    Log.Information($"APPLICATIONINSIGHTS_CONNECTION_STRING: {(string.IsNullOrEmpty(connectionString) ? "NOT SET" : "SET")}");
    
    // Debug: Check GitHub environment variables
    Log.Information($"GITHUB__APPID: {(string.IsNullOrEmpty(githubAppId) ? "NOT SET" : "SET")}");
    Log.Information($"GITHUB__CLIENTID: {(string.IsNullOrEmpty(githubClientId) ? "NOT SET" : "SET")}");
    Log.Information($"GITHUB__CLIENTSECRET: {(string.IsNullOrEmpty(githubClientSecret) ? "NOT SET" : "SET")}");
    Log.Information($"GITHUB__PRIVATEKEYCONTENT: {(string.IsNullOrEmpty(githubPrivateKey) ? "NOT SET" : "SET")}");
    Log.Information($"GITHUB__INSTALLATIONID: {(string.IsNullOrEmpty(githubInstallationId) ? "NOT SET" : "SET")}");

    if (!string.IsNullOrEmpty(connectionString))
    {
        Log.Information("Will use APPLICATIONINSIGHTS_CONNECTION_STRING for Application Insights configuration");
    }
    else if (!string.IsNullOrEmpty(instrumentationKey))
    {
        Log.Information("Will use APPINSIGHTS_INSTRUMENTATIONKEY for Application Insights configuration");
    }
    else
    {
        Log.Warning("No Application Insights environment variables found - logging will be limited to console/file");
    }

    if (!string.IsNullOrEmpty(githubAppId) || !string.IsNullOrEmpty(githubClientId) || 
        !string.IsNullOrEmpty(githubClientSecret) || !string.IsNullOrEmpty(githubPrivateKey) || 
        !string.IsNullOrEmpty(githubInstallationId))
    {
        Log.Information("Will use GitHub environment variables for GitHub configuration (production)");
    }
    else
    {
        Log.Information("Will use appsettings.GitHub.Development.json for GitHub configuration (local development)");
    }

    // Check for other possible environment variable names
    try
    {
        var allEnvVars = Environment.GetEnvironmentVariables();
        if (allEnvVars?.Keys != null)
        {
            var appInsightsVars = allEnvVars.Keys.Cast<string>()
                .Where(k => k != null && (k.Contains("APPINSIGHTS") || k.Contains("INSTRUMENTATION") || k.Contains("APPLICATIONINSIGHTS")))
                .ToList();
            
            Log.Information($"Found {appInsightsVars.Count} Application Insights related environment variables: {string.Join(", ", appInsightsVars)}");
        }
        else
        {
            Log.Warning("Could not access environment variables");
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Error while checking environment variables");
    }

    // Debug: Check Serilog configuration
    Log.Information("=== SERILOG CONFIGURATION DEBUG ===");
    var serilogSection = configuration.GetSection("Serilog");
    Log.Information($"Serilog section exists: {serilogSection.Exists()}");
    
    if (serilogSection.Exists())
    {
        var writeToSection = serilogSection.GetSection("WriteTo");
        if (writeToSection.Exists())
        {
            Log.Information($"Serilog WriteTo count: {writeToSection.GetChildren().Count()}");
            
            foreach (var sink in writeToSection.GetChildren())
            {
                var sinkName = sink["Name"] ?? "Unknown";
                Log.Information($"Sink: {sinkName}");
            }
        }
        else
        {
            Log.Warning("Serilog WriteTo section not found in configuration");
        }
    }
    else
    {
        Log.Warning("Serilog section not found in configuration");
    }

    // Debug: Check GitHub configuration
    Log.Information("=== GITHUB CONFIGURATION DEBUG ===");
    var githubSection = configuration.GetSection("GitHub");
    
    if (githubSection.Exists())
    {
        var appId = githubSection["AppId"];
        var clientId = githubSection["ClientId"];
        var installationId = githubSection["InstallationId"];
        
        Log.Information($"GitHub AppId configured: {(!string.IsNullOrEmpty(appId) ? "YES" : "NO")}");
        Log.Information($"GitHub ClientId configured: {(!string.IsNullOrEmpty(clientId) ? "YES" : "NO")}");
        Log.Information($"GitHub InstallationId configured: {(!string.IsNullOrEmpty(installationId) ? "YES" : "NO")}");
        Log.Information($"GitHub PrivateKey configured: {(!string.IsNullOrEmpty(githubSection["PrivateKeyContent"]) ? "YES" : "NO")}");
        
        // Clarify configuration source
        if (!string.IsNullOrEmpty(githubAppId) || !string.IsNullOrEmpty(githubClientId) || 
            !string.IsNullOrEmpty(githubClientSecret) || !string.IsNullOrEmpty(githubPrivateKey) || 
            !string.IsNullOrEmpty(githubInstallationId))
        {
            Log.Information("Configuration source: Azure Web App environment variables (production)");
        }
        else if (!string.IsNullOrEmpty(appId) || !string.IsNullOrEmpty(clientId) || 
                 !string.IsNullOrEmpty(installationId) || !string.IsNullOrEmpty(githubSection["PrivateKeyContent"]))
        {
            Log.Information("Configuration source: appsettings.GitHub.Development.json (local development)");
        }
        else
        {
            Log.Information("Configuration source: appsettings.GitHub.json (base configuration, no secrets)");
        }
    }
    else
    {
        Log.Warning("GitHub section not found in configuration");
    }

    // Application Insights is configured via Serilog above - don't add it again here
    // builder.Services.AddApplicationInsightsTelemetry(); // DISABLED to prevent conflicts

    builder.Services.AddDbContext<GainItDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("GainItPostgresDb")));

    builder.Services.Configure<AzureSearchOptions>(
        builder.Configuration.GetSection("AzureSearch"));
    builder.Services.Configure<OpenAIOptions>(
        builder.Configuration.GetSection("OpenAI"));
    builder.Services.Configure<AcsEmailOptions>(
        builder.Configuration.GetSection("ACS:Email"));
    builder.Services.AddSignalR()
    .AddAzureSignalR(
        builder.Configuration["SignalR:ConnectionString"]);
    builder.Services.Configure<JoinRequestOptions>(
        builder.Configuration.GetSection("JoinRequests"));
    builder.Services.Configure<GitHubOptions>(
        builder.Configuration.GetSection("GitHub"));

    builder.Services.AddSingleton(sp =>
    {
        var opts = sp.GetRequiredService<IOptions<AzureSearchOptions>>().Value;
        return new SearchClient(
            new Uri(opts.Endpoint),
            opts.IndexName,
            new Azure.AzureKeyCredential(opts.ApiKey)
        );
        
    });

    builder.Services.AddSingleton(sp =>
    {
        var opts = sp.GetRequiredService<IOptions<OpenAIOptions>>().Value;
        return new AzureOpenAIClient(
            new Uri(opts.Endpoint),
            new AzureKeyCredential(opts.ApiKey)
        );
    });

    builder.Services.AddSingleton(sp =>
    {
        var cfg = sp.GetRequiredService<IOptions<AcsEmailOptions>>().Value;
        // cfg.ConnectionString נטען מ-ACS__Email__ConnectionString (App Settings בענן)
        return new Azure.Communication.Email.EmailClient(cfg.ConnectionString);
    });

    var b2c = builder.Configuration.GetSection("AzureAdB2C");
    var authority = $"{b2c["Instance"]!.TrimEnd('/')}/{b2c["Domain"]}/{b2c["SignUpSignInPolicyId"]}/v2.0";

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(o =>
        {
            o.Authority = authority;                                            // https://gainitauth.ciamlogin.com/.../GainitauthUF1/v2.0
            o.Audience = b2c["Audience"];                                      // api://gainitwebapp-...azurewebsites.net
            o.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = "name",
                ValidateIssuer = true,
                ValidateAudience = true
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAccessAsUser", policy =>
            policy.RequireAssertion(ctx =>
            {
                // scp is a space-delimited list in delegated-user tokens
                var scp = ctx.User.FindFirst("scp")?.Value ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(scp))
                {
                    return scp.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                              .Contains("access_as_user", StringComparer.Ordinal);
                }

                // optional fallback for app roles (client credentials / roles tokens)
                var roles = ctx.User.FindAll("roles").Select(c => c.Value);
                return roles.Contains("access_as_user", StringComparer.Ordinal);
            })
        );
    });

            // Add services to the container.
        builder.Services.AddScoped<IProjectService, ProjectService>();
        builder.Services.AddScoped<IUserProfileService, UserProfileService>();
        builder.Services.AddScoped<IProjectMatchingService, ProjectMatchingService>();
        builder.Services.AddScoped<IEmailSender, AcsEmailSender>();
        builder.Services.AddSingleton<IUserIdProvider, JwtUserIdProvider>();
        builder.Services.AddScoped<IJoinRequestService, JoinRequestService>();
        builder.Services.AddScoped<IMilestoneService, MilestoneService>();
        builder.Services.AddScoped<ITaskService, TaskService>();
        builder.Services.AddScoped<ITaskNotificationService, TaskNotificationService>();
        builder.Services.AddScoped<IPlanningService, PlanningService>();
    
        // GitHub Services
        builder.Services.AddScoped<IGitHubService, GitHubService>();
        builder.Services.AddScoped<IGitHubApiClient, GitHubApiClient>();
        builder.Services.AddScoped<IGitHubAnalyticsService, GitHubAnalyticsService>();

        // Add HTTP client for GitHub API
        builder.Services.AddHttpClient<IGitHubApiClient, GitHubApiClient>();


    // Add health checks
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "database", "sql" });

    // Configure JSON serialization to handle circular references without adding $id/$values
    builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        
        // Log the actual JSON settings being used
        Log.Information("=== JSON Settings Applied ===");
        Log.Information($"ReferenceHandler: {options.JsonSerializerOptions.ReferenceHandler}");
        Log.Information($"WriteIndented: {options.JsonSerializerOptions.WriteIndented}");
        Log.Information($"PropertyNamingPolicy: {options.JsonSerializerOptions.PropertyNamingPolicy}");
    });

    // Force System.Text.Json globally to prevent Entity Framework from using JSON.NET
    builder.Services.Configure<JsonOptions>(options =>
    {
        options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.SerializerOptions.WriteIndented = true;
    });

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        options.IncludeXmlComments(xmlPath);

        // JWT Bearer in Swagger (Authorize button)
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter: Bearer {your_access_token}"
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("signalr-cors", p => p
            .WithOrigins("http://localhost:5173", "http://localhost:3000", "https://gray-moss-04b923a10.2.azurestaticapps.net")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
    });

    var app = builder.Build();

    // Test logging output for Azure Log Stream
    Log.Information("=== APPLICATION TEST: Application built successfully ===");
    Log.Information($"=== .NET Version: {Environment.Version} ===");
    Log.Information($"=== Target Framework: {AppContext.TargetFrameworkName} ===");
    Log.Information($"=== Environment: {app.Environment.EnvironmentName} ===");
    Log.Information($"Running on .NET {Environment.Version} in {app.Environment.EnvironmentName} environment");

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCorrelationId(); // Add correlation ID to all requests
    app.UsePerformanceMonitoring(); // Monitor performance and memory usage
    app.UseMiddleware<RequestLoggingMiddleware>(); //logs starts
    app.UseHttpsRedirection(); //redirects to https
    app.UseCors("signalr-cors");

    if (!app.Environment.IsDevelopment())
    {
        app.UseAuthentication(); //authenticates the request
        app.UseAuthorization(); //authorizes the request
    }
    else
    {
        Log.Warning("Development environment detected: skipping authentication/authorization middleware for local testing");
    }

    app.MapControllers(); //maps the controllers to the request
    app.MapHub<NotificationsHub>("/hubs/notifications");

    // Add health check endpoint
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds
                })
            });
            await context.Response.WriteAsync(result);
        }
    }); 

    // Add a simple test endpoint to verify logging
    app.MapGet("/test-logging", () =>
    {
        Log.Debug("Test DEBUG log from minimal API endpoint");
        Log.Information("Test INFORMATION log from minimal API endpoint");
        Log.Warning("Test WARNING log from minimal API endpoint");
        Log.Error("Test ERROR log from minimal API endpoint");
        
        return "Logging test completed - check Application Insights for log messages";
    });



    // Seed the database with initial data
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<GainItDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<GainItDbContext>>();
        GainItDbContextSeeder.SeedData(context, logger);
    }

    Log.Information("GainIt.API application started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "GainIt.API application failed to start");
}
finally
{
    Log.CloseAndFlush();
}