using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Storage.Blobs;
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
using Microsoft.Extensions.Caching.Memory;
using GainIt.API.Services.Forum.Implementations;
using GainIt.API.Services.Forum.Interfaces;
using GainIt.API.Services.FileUpload.Implementations;
using GainIt.API.Services.FileUpload.Interfaces;
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
    .AddEnvironmentVariables();

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
    
    Log.Information("Application Insights sink added using connection string");
}
else if (!string.IsNullOrWhiteSpace(instrumentationKey))
{
    // Fallback to instrumentation key only
    var cleanInstrumentationKey = instrumentationKey.Trim().Replace("\"", "").Replace("'", "");
    var appInsightsConnectionString = $"InstrumentationKey={cleanInstrumentationKey}";
    
    loggerConfig.WriteTo.ApplicationInsights(
        appInsightsConnectionString,
        new Serilog.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter());
    
    Log.Information("Application Insights sink added using instrumentation key");
}
else
{
    Log.Warning("No Application Insights environment variables found");
}

// Create the logger only once after all configuration is complete
Log.Logger = loggerConfig.CreateLogger();



try
{
    Log.Information("Starting GainIt.API application...");

    var builder = WebApplication.CreateBuilder(args);

    // Clear default logging providers to prevent duplicate logs
    builder.Logging.ClearProviders();

    // Use the pre-configured Serilog
    builder.Host.UseSerilog();







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

    // Add Azure Storage configuration
    builder.Services.Configure<AzureStorageOptions>(
        builder.Configuration.GetSection(AzureStorageOptions.SectionName));

    // Add BlobServiceClient
    builder.Services.AddSingleton<BlobServiceClient>(sp =>
    {
        var options = sp.GetRequiredService<IOptions<AzureStorageOptions>>().Value;
        return new BlobServiceClient(options.ConnectionString);
    });

    // Add File Upload Service
    builder.Services.AddScoped<IFileUploadService, FileUploadService>();

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

    // Get Azure AD B2C configuration
    var b2cFromBuilder = builder.Configuration.GetSection("AzureAdB2C");
    var b2cFromSerilog = configuration.GetSection("AzureAdB2C");
    var b2c = b2cFromSerilog.Exists() ? b2cFromSerilog : b2cFromBuilder;
    
    // Check if configuration is missing
    if (string.IsNullOrWhiteSpace(b2c["Instance"]) || 
        string.IsNullOrWhiteSpace(b2c["Domain"]) || 
        string.IsNullOrWhiteSpace(b2c["Audience"]))
    {
        Log.Error("Azure AD B2C configuration is missing or incomplete");
        Log.Warning("Continuing with default Azure AD B2C configuration - authentication may fail");
        
        // Set default values to prevent null reference exceptions
        if (string.IsNullOrWhiteSpace(b2c["Instance"])) b2c["Instance"] = "https://559c1923-19d9-428c-a51a-36c92e884239.ciamlogin.com/";
        if (string.IsNullOrWhiteSpace(b2c["Domain"])) b2c["Domain"] = "559c1923-19d9-428c-a51a-36c92e884239";
        if (string.IsNullOrWhiteSpace(b2c["Audience"])) b2c["Audience"] = "api://gainitwebapp-dvhfcxbkezgyfwf6.israelcentral-01.azurewebsites.net";
    }
    
    // Build authority without policy to match tokens like .../{tenantId}/v2.0
    var tenantId = b2c["Domain"];
    var baseAuthority = $"{b2c["Instance"]!.TrimEnd('/')}/{tenantId}/v2.0";
    var policy = b2c["SignUpSignInPolicyId"];
    var policyAuthority = !string.IsNullOrWhiteSpace(policy)
        ? $"{b2c["Instance"]!.TrimEnd('/')}/{tenantId}/{policy}/v2.0"
        : null;
    Log.Information("AUTH CONFIG VERSION v7.1 - base issuer without policy");
    Log.Information("Authority: {Authority}", baseAuthority);

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(o =>
        {
        o.Authority = baseAuthority;
        o.Audience = b2c["Audience"];
        o.IncludeErrorDetails = true;
            o.TokenValidationParameters = new TokenValidationParameters
            {
            NameClaimType = "preferred_username",
                ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = baseAuthority,
            ValidIssuers = policyAuthority is not null ? new[] { baseAuthority, policyAuthority } : new[] { baseAuthority },
            ValidAudiences = new[] { b2c["Audience"]!, "ee13203e-e81d-48cb-8402-422cace331dc" }
        };
        

        
        // JWT validation events
        o.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Log.Error("JWT Authentication failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Log.Information("JWT Token validated successfully for user: {User}", context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                Log.Warning("JWT Challenge (401): {Error} {Description}", ctx.Error, ctx.ErrorDescription);
                return Task.CompletedTask;
            },
            OnForbidden = ctx =>
            {
                Log.Warning("JWT Forbidden (403): user lacks required scopes/roles");
                return Task.CompletedTask;
            }
        };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAccessAsUser", policy =>
            policy.RequireAssertion(ctx =>
            {
                // In Development, always allow access
                var httpContext = ctx.Resource as HttpContext;
                if (httpContext?.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment() == true)
                {
                    return true;
                }

                // Support both 'scp' and the full URI scope claim
                var scp = ctx.User.FindFirst("scp")?.Value;
                var scopeUri = ctx.User.FindFirst("http://schemas.microsoft.com/identity/claims/scope")?.Value;
                var scopesCombined = string.Join(' ', new[] { scp, scopeUri }.Where(s => !string.IsNullOrWhiteSpace(s)));
                if (!string.IsNullOrWhiteSpace(scopesCombined))
                {
                    return scopesCombined.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                              .Contains("access_as_user", StringComparer.Ordinal);
                }

                // optional fallback for app roles (client credentials / roles tokens)
                var roles = ctx.User.FindAll("roles").Select(c => c.Value);
                return roles.Contains("access_as_user", StringComparer.Ordinal);
            })
        );

        // Add admin policy for admin-only endpoints
        options.AddPolicy("RequireAdminAccess", policy =>
            policy.RequireAssertion(ctx =>
            {
                // In Development, always allow access
                var httpContext = ctx.Resource as HttpContext;
                if (httpContext?.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment() == true)
                {
                    return true;
                }

                // Check for admin role
                var roles = ctx.User.FindAll("roles").Select(c => c.Value);
                return roles.Contains("admin", StringComparer.OrdinalIgnoreCase);
            })
        );
    });

    // Add services to the container.
    builder.Services.AddScoped<IProjectService, ProjectService>();
    builder.Services.AddScoped<IUserProfileService, UserProfileService>();
    builder.Services.AddScoped<IUserSummaryService, UserSummaryService>();
    builder.Services.AddMemoryCache();
    builder.Services.AddScoped<IProjectMatchingService, ProjectMatchingService>();
    builder.Services.AddScoped<IProjectConfigurationService, ProjectConfigurationService>();
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

    // Forum Services
    builder.Services.AddScoped<IForumService, ForumService>();
    builder.Services.AddScoped<IForumNotificationService, ForumNotificationService>();

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
            .WithOrigins("http://localhost:5173", "http://localhost:3000", "https://letsgainit.com", "https://www.letsgainit.com", "https://gray-moss-04b923a10.2.azurestaticapps.net")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
    });

    var app = builder.Build();

    Log.Information("Application built successfully - Environment: {Environment}", app.Environment.EnvironmentName);

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

    // Use authentication/authorization middleware based on environment
    if (!app.Environment.IsDevelopment())
    {
        app.UseAuthentication(); //authenticates the request
        app.UseAuthorization(); //authorizes the request
        Log.Information("Production environment: authentication and authorization middleware enabled");
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



    // Seed the database with initial data
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<GainItDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<GainItDbContext>>();
        var projectConfigService = scope.ServiceProvider.GetRequiredService<IProjectConfigurationService>();
        
        // Simple synchronous seeding
        GainItDbContextSeeder.SeedData(context, projectConfigService, logger);
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