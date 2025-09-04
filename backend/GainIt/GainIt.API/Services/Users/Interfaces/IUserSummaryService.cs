using System;
using System.Threading.Tasks;

namespace GainIt.API.Services.Users.Interfaces
{
    public interface IUserSummaryService
    {
        Task<string> GetUserSummaryAsync(Guid userId);
        Task<object> GetUserDashboardAsync(Guid userId);
    }
}


