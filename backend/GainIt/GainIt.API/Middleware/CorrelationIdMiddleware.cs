using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GainIt.API.Middleware
{
    /// <summary>
    /// Middleware for adding correlation IDs to requests for distributed tracing.
    /// Extracts existing X-Correlation-ID header or generates a new GUID.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate r_next;
        private readonly ILogger<CorrelationIdMiddleware> r_logger;
        private const string CorrelationIdHeader = "X-Correlation-ID";

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationIdMiddleware"/> class.
        /// </summary>
        /// <param name="i_next">The next middleware in the pipeline.</param>
        /// <param name="i_logger">The logger for recording correlation events.</param>
        public CorrelationIdMiddleware(RequestDelegate i_next, ILogger<CorrelationIdMiddleware> i_logger)
        {
            r_next = i_next;
            r_logger = i_logger;
        }

        /// <summary>
        /// Processes the HTTP request and adds correlation ID to headers and logs.
        /// </summary>
        /// <param name="i_context">The HTTP context for the request.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext i_context)
        {
            var correlationId = GetOrCreateCorrelationId(i_context);

            // Add correlation ID to the response headers
            i_context.Response.Headers[CorrelationIdHeader] = correlationId;

            // Add correlation ID to the log context
            using (r_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            }))
            {
                r_logger.LogDebug("Request started with correlation ID: {CorrelationId}", correlationId);

                try
                {
                    await r_next(i_context);
                }
                catch (Exception ex)
                {
                    r_logger.LogError(ex, "Request failed with correlation ID: {CorrelationId}", correlationId);
                    throw;
                }
                finally
                {
                    r_logger.LogDebug("Request completed with correlation ID: {CorrelationId}, StatusCode: {StatusCode}",
                        correlationId, i_context.Response.StatusCode);
                }
            }
        }

        /// <summary>
        /// Gets existing correlation ID from request headers or generates a new one.
        /// </summary>
        /// <param name="i_context">The HTTP context containing request headers.</param>
        /// <returns>The correlation ID as a string.</returns>
        private static string GetOrCreateCorrelationId(HttpContext i_context)
        {
            // Check if correlation ID is already in the request headers
            if (i_context.Request.Headers.TryGetValue(CorrelationIdHeader, out var existingCorrelationId))
            {
                return existingCorrelationId.ToString();
            }

            // Generate a new correlation ID
            return Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Extension methods for <see cref="CorrelationIdMiddleware"/>.
    /// </summary>
    public static class CorrelationIdMiddlewareExtensions
    {
        /// <summary>
        /// Adds correlation ID middleware to the application pipeline.
        /// </summary>
        /// <param name="i_builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder i_builder)
        {
            return i_builder.UseMiddleware<CorrelationIdMiddleware>();
        }
    }
}