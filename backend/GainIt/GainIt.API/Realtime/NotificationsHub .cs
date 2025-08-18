using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GainIt.API.Realtime
{
    [Authorize(Policy = "RequireAccessAsUser")]
    public class NotificationsHub : Hub
    {
        private readonly ILogger<NotificationsHub> _logger;

        public NotificationsHub(ILogger<NotificationsHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Hub connected. User={UserId}, ConnectionId={ConnId}",
                Context.UserIdentifier, Context.ConnectionId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception is null)
            {
                _logger.LogInformation("Hub disconnected. User={UserId}, ConnectionId={ConnId}",
                    Context.UserIdentifier, Context.ConnectionId);
            }
            else
            {
                _logger.LogWarning(exception,
                    "Hub disconnected with error. User={UserId}, ConnectionId={ConnId}",
                    Context.UserIdentifier, Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
