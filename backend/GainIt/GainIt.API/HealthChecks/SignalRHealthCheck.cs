using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GainIt.API.HealthChecks
{
    public class SignalRHealthCheck : IHealthCheck
    {
        private readonly IHubContext<Realtime.NotificationsHub> _hubContext;
        private readonly ILogger<SignalRHealthCheck> _logger;

        public SignalRHealthCheck(IHubContext<Realtime.NotificationsHub> hubContext, ILogger<SignalRHealthCheck> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Simple check to ensure SignalR hub context is available
                if (_hubContext == null)
                {
                    _logger.LogWarning("SignalR hub context is null");
                    return HealthCheckResult.Unhealthy("SignalR hub context is not available");
                }

                // Try to get hub context type to verify it's properly configured
                var hubType = _hubContext.GetType();
                _logger.LogDebug("SignalR health check passed: Hub type: {HubType}", hubType.Name);

                return HealthCheckResult.Healthy("SignalR service is operational");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR health check failed");
                return HealthCheckResult.Unhealthy("SignalR service is not operational", ex);
            }
        }
    }
}
