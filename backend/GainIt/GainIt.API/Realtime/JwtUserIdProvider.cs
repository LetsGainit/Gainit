using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace GainIt.API.Realtime
{
    public sealed class JwtUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            var user = connection.User;
            return TryGetClaim(user, "oid", ClaimTypes.NameIdentifier)
                ?? TryGetClaim(user, "sub")
                ?? TryGetClaim(user, ClaimTypes.NameIdentifier)
                ?? TryGetClaim(user, "http://schemas.microsoft.com/identity/claims/objectidentifier")
                ?? TryGetClaim(user, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        }

        private static string? TryGetClaim(ClaimsPrincipal? user, params string[] types)
        {
            if (user == null) return null;
            
            foreach (var type in types)
            {
                var value = user.FindFirstValue(type);
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
            return null;
        }
    }
}
