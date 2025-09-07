using System;
using System.Threading.Tasks;

namespace GainIt.API.Services.Users.Interfaces
{
    public interface IUserSummaryService
    {
        Task<string> GetUserSummaryAsync(Guid userId, bool forceRefresh = false);
        Task<object> GetUserDashboardAsync(Guid userId);
    }
}


