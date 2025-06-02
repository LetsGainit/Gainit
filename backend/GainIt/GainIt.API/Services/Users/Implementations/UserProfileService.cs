using GainIt.API.Data;
using GainIt.API.DTOs.ViewModels.Users;
using GainIt.API.Models.Users.Gainers;
using GainIt.API.Models.Users.Mentors;
using GainIt.API.Models.Users.Nonprofits;
using GainIt.API.Services.Users.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GainIt.API.Services.Users.Implementations
{
    public class UserProfileService : IUserProfileService
    {
        private readonly GainItDbContext _dbContext;

        public UserProfileService(GainItDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Gainer?> GetGainerByIdAsync(Guid i_GainerId)
        {
            return await _dbContext.Gainers
                .Include(gainer => gainer.TechExpertise)
                .Include(gainer => gainer.ParticipatedProjects)
                .Include(gainer => gainer.Achievements)
                .FirstOrDefaultAsync(g => g.UserId == i_GainerId);
        }

        public async Task<Mentor?> GetMentorByIdAsync(Guid i_MentorId)
        {
            return await _dbContext.Mentors
                .Include(m => m.TechExpertise)
                .Include(m => m.MentoredProjects)
                .Include(m => m.Achievements)
                .FirstOrDefaultAsync(m => m.UserId == i_MentorId);
        }

        public async Task<NonprofitOrganization?> GetNonprofitByIdAsync(Guid i_NonprofitId)
        {
            return await _dbContext.Nonprofits
                .Include(n => n.NonprofitExpertise)
                .Include(n => n.OwnedProjects)
                .FirstOrDefaultAsync(n => n.UserId == i_NonprofitId);
        }
    }
}
