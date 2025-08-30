using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using GainIt.API.Data;

namespace GainIt.API.HealthChecks
{
    /// <summary>
    /// Health check for monitoring database connectivity and performance.
    /// Tests database connection and measures response time.
    /// 
    /// Access this health check via: GET /health
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly GainItDbContext r_dbContext;
        private readonly ILogger<DatabaseHealthCheck> r_logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseHealthCheck"/> class.
        /// </summary>
        /// <param name="dbContext">The database context for database operations.</param>
        /// <param name="logger">The logger for recording health check events.</param>
        public DatabaseHealthCheck(GainItDbContext dbContext, ILogger<DatabaseHealthCheck> logger)
        {
            r_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            r_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Performs the database health check asynchronously.
        /// </summary>
        /// <param name="context">The health check context.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>
        /// <see cref="HealthCheckResult.Healthy"/> if database is accessible,
        /// <see cref="HealthCheckResult.Unhealthy"/> if not accessible.
        /// </returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            r_logger.LogDebug("Starting database health check");

            try
            {
                var startTime = DateTime.UtcNow;

                // Test database connectivity
                await r_dbContext.Database.CanConnectAsync(cancellationToken);

                var duration = DateTime.UtcNow - startTime;
                r_logger.LogInformation("Database health check completed successfully: Duration={Duration}ms", duration.TotalMilliseconds);

                return HealthCheckResult.Healthy("Database is accessible", new Dictionary<string, object>
                {
                    ["Duration"] = duration.TotalMilliseconds,
                    ["Timestamp"] = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Database health check failed");
                return HealthCheckResult.Unhealthy("Database is not accessible", ex);
            }
        }
    }
}