using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Search.Documents;
using GainIt.API.Data;
using GainIt.API.Middleware;
using GainIt.API.Options;
using GainIt.API.Services.Projects.Implementations;
using GainIt.API.Services.Projects.Interfaces;
using GainIt.API.Services.Users.Implementations;
using GainIt.API.Services.Users.Interfaces;
using GainIt.API.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;


// Build configuration first
var configBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var configuration = configBuilder.Build();

// Bootstrap Serilog from configuration only (avoid adding sinks programmatically here to prevent duplicates)
var loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration);

Log.Logger = loggerConfig.CreateLogger();

try
{
    Log.Information("Starting GainIt.API application...");

    var builder = WebApplication.CreateBuilder(args);

    // Clear default logging providers to prevent duplicate logs
    builder.Logging.ClearProviders();

    // Configure Serilog for the application
    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext();

            // Configure Application Insights during Serilog setup
    // PRIORITIZE instrumentation key over complex connection string to avoid parsing errors
    var appInsightsConnectionString = context.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]
        ?? Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY")
        ?? context.Configuration["ApplicationInsights:ConnectionString"]
        ?? Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");

        if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
        {
            // If it's just an instrumentation key, format it properly
            if (!appInsightsConnectionString.Contains(";") && !appInsightsConnectionString.Contains("="))
            {
                appInsightsConnectionString = $"InstrumentationKey={appInsightsConnectionString}";
            }

            try
            {
                loggerConfiguration.WriteTo.ApplicationInsights(
                    appInsightsConnectionString,
                    new Serilog.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter());
                
                // Log success (this will go to console/file initially)
                Console.WriteLine($"✅ Application Insights sink added successfully using: {appInsightsConnectionString.Substring(0, Math.Min(50, appInsightsConnectionString.Length))}...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to add Application Insights sink: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("⚠️ No Application Insights configuration found - logging will be limited to console/file");
        }
    });

    // Add Application Insights
    builder.Services.AddApplicationInsightsTelemetry();

    builder.Services.AddDbContext<GainItDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("GainItPostgresDb")));

    builder.Services.Configure<AzureSearchOptions>(
        builder.Configuration.GetSection("AzureSearch"));
    builder.Services.Configure<OpenAIOptions>(
        builder.Configuration.GetSection("OpenAI"));

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
        // ????????? ??? ?????: ????? ?????? ???? ?? ?-scope ?? ?-API
        options.AddPolicy("RequireAccessAsUser",
            p => p.RequireClaim("scp", "access_as_user"));
    });

    // Add services to the container.
    builder.Services.AddScoped<IProjectService, ProjectService>();
    builder.Services.AddScoped<IUserProfileService, UserProfileService>();
    builder.Services.AddScoped<IProjectMatchingService, ProjectMatchingService>();

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

    var app = builder.Build();

    // Test logging output for Azure Log Stream
    Log.Information("=== APPLICATION TEST: Application built successfully ===");
    Log.Information($"=== .NET Version: {Environment.Version} ===");
    Log.Information($"=== Target Framework: {AppContext.TargetFrameworkName} ===");
    Log.Information($"=== Environment: {app.Environment.EnvironmentName} ===");
    Log.Information("Application built successfully - logging is working!");
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
    app.UseAuthentication(); //authenticates the request
    app.UseAuthorization(); //authorizes the request
    app.MapControllers(); //maps the controllers to the request
    
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
