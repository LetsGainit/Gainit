using Microsoft.AspNetCore.Http;
using Serilog;
using System.Diagnostics;

namespace GainIt.API.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate r_next;
        private readonly ILogger<RequestLoggingMiddleware> r_logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            r_next = next;
            r_logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString();
            
            // Add request ID to the context for correlation
            context.Items["RequestId"] = requestId;
            
            // Log request start
            r_logger.LogInformation(
                "Request started: {RequestId} {Method} {Path} from {IP}",
                requestId,
                context.Request.Method,
                context.Request.Path,
                context.Connection.RemoteIpAddress);

            try
            {
                await r_next(context);
            }
            catch (Exception ex)
            {
                r_logger.LogError(
                    ex,
                    "Request failed: {RequestId} {Method} {Path} - {ErrorMessage}",
                    requestId,
                    context.Request.Method,
                    context.Request.Path,
                    ex.Message);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                
                // Log request completion
                r_logger.LogInformation(
                    "Request completed: {RequestId} {Method} {Path} - Status: {StatusCode} - Duration: {Duration}ms",
                    requestId,
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
} 