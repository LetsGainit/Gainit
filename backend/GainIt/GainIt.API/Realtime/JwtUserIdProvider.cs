using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace GainIt.API.Realtime
{
    public sealed class JwtUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            var user = connection.User;
            return user?.FindFirst("oid")?.Value
                ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user?.FindFirst("sub")?.Value;
        }
    }
}
