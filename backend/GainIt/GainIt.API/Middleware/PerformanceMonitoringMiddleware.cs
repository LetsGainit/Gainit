using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace GainIt.API.Middleware
{
    /// <summary>
    /// Middleware for monitoring request performance and memory usage.
    /// Logs warnings for slow requests (>500ms) or high memory usage (>10MB).
    /// </summary>
    public class PerformanceMonitoringMiddleware
    {
        private readonly RequestDelegate r_next;
        private readonly ILogger<PerformanceMonitoringMiddleware> r_logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceMonitoringMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger for recording performance metrics.</param>
        public PerformanceMonitoringMiddleware(RequestDelegate next, ILogger<PerformanceMonitoringMiddleware> logger)
        {
            r_next = next;
            r_logger = logger;
        }

        /// <summary>
        /// Processes the HTTP request and monitors its performance.
        /// </summary>
        /// <param name="context">The HTTP context for the request.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var initialMemory = GC.GetTotalMemory(false);

            try
            {
                await r_next(context);
            }
            finally
            {
                stopwatch.Stop();
                var finalMemory = GC.GetTotalMemory(false);
                var memoryDelta = finalMemory - initialMemory;

                // Log performance metrics for slow requests (>500ms) or high memory usage (>10MB)
                if (stopwatch.ElapsedMilliseconds > 500 || memoryDelta > 10 * 1024 * 1024)
                {
                    r_logger.LogWarning("Performance alert: Path={Path}, Duration={Duration}ms, MemoryDelta={MemoryDelta}bytes, StatusCode={StatusCode}",
                        context.Request.Path, stopwatch.ElapsedMilliseconds, memoryDelta, context.Response.StatusCode);
                }
                else
                {
                    r_logger.LogDebug("Request performance: Path={Path}, Duration={Duration}ms, MemoryDelta={MemoryDelta}bytes, StatusCode={StatusCode}",
                        context.Request.Path, stopwatch.ElapsedMilliseconds, memoryDelta, context.Response.StatusCode);
                }
            }
        }
    }

    /// <summary>
    /// Extension methods for <see cref="PerformanceMonitoringMiddleware"/>.
    /// </summary>
    public static class PerformanceMonitoringMiddlewareExtensions
    {
        /// <summary>
        /// Adds performance monitoring middleware to the application pipeline.
        /// </summary>
        /// <param name="i_builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UsePerformanceMonitoring(this IApplicationBuilder i_builder)
        {
            return i_builder.UseMiddleware<PerformanceMonitoringMiddleware>();
        }
    }
} 