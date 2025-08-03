using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using GainIt.API.Data;
using GainIt.API.Middleware;
using GainIt.API.Options;
using GainIt.API.Services.Projects.Implementations;
using GainIt.API.Services.Projects.Interfaces;
using GainIt.API.Services.Users.Implementations;
using GainIt.API.Services.Users.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using System.Reflection;
using Azure.Core;
using System.Text.Json.Serialization;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting GainIt.API application...");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog for the application
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/gainit-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30));

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
        return new OpenAIClient(
            new Uri(opts.Endpoint),
            new AzureKeyCredential(opts.ApiKey)
        );
    });

    // Add services to the container.
    builder.Services.AddScoped<IProjectService, ProjectService>();
    builder.Services.AddScoped<IUserProfileService, UserProfileService>();
    builder.Services.AddScoped<IProjectMatchingService, ProjectMatchingService>();

    builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        options.IncludeXmlComments(xmlPath);
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseMiddleware<RequestLoggingMiddleware>(); //logs starts
    app.UseHttpsRedirection(); //redirects to https
    app.UseAuthorization(); //authorizes the request
    app.MapControllers(); //maps the controllers to the request 

    // Seed the database with initial data
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<GainItDbContext>();
        GainItDbContextSeeder.SeedData(context);
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
