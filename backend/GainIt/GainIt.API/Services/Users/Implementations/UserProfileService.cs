using GainIt.API.Data;
using GainIt.API.DTOs.Requests.Users;
using GainIt.API.DTOs.ViewModels.Users;
using GainIt.API.Models.Users;
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

        public async Task<Gainer> GetGainerByIdAsync(Guid userId)
        {
            return await _dbContext.Gainers
                .Include(g => g.TechExpertise)
                .Include(g => g.ParticipatedProjects)
                .Include(g => g.Achievements)
                .FirstOrDefaultAsync(g => g.UserId == userId);
        }

        public async Task<Mentor> GetMentorByIdAsync(Guid userId)
        {
            return await _dbContext.Mentors
                .Include(m => m.TechExpertise)
                .Include(m => m.MentoredProjects)
                .Include(m => m.Achievements)
                .FirstOrDefaultAsync(m => m.UserId == userId);
        }

        public async Task<NonprofitOrganization> GetNonprofitByIdAsync(Guid userId)
        {
            return await _dbContext.Nonprofits
                .Include(n => n.NonprofitExpertise)
                .Include(n => n.OwnedProjects)
                .FirstOrDefaultAsync(n => n.UserId == userId);
        }

        public async Task<Gainer> UpdateGainerProfileAsync(Guid userId, GainerProfileUpdateDto updateDto)
        {
            var gainer = await GetGainerByIdAsync(userId);
            if (gainer == null)
                throw new KeyNotFoundException($"Gainer with ID {userId} not found");

            // Update base user properties
            gainer.FullName = updateDto.FullName;
            gainer.Biography = updateDto.Biography;
            gainer.FacebookPageURL = updateDto.FacebookPageURL;
            gainer.LinkedInURL = updateDto.LinkedInURL;
            gainer.GitHubURL = updateDto.GitHubURL;
            gainer.ProfilePictureURL = updateDto.ProfilePictureURL;

            // Update Gainer-specific properties
            gainer.CurrentRole = updateDto.CurrentRole;
            gainer.YearsOfExperience = updateDto.YearsOfExperience;
            gainer.EducationLevel = updateDto.EducationLevel;
            gainer.CareerGoals = updateDto.CareerGoals;

            await _dbContext.SaveChangesAsync();
            return gainer;
        }

        public async Task<Mentor> UpdateMentorProfileAsync(Guid userId, MentorProfileUpdateDto updateDto)
        {
            var mentor = await GetMentorByIdAsync(userId);
            if (mentor == null)
                throw new KeyNotFoundException($"Mentor with ID {userId} not found");

            // Update base user properties
            mentor.FullName = updateDto.FullName;
            mentor.Biography = updateDto.Biography;
            mentor.FacebookPageURL = updateDto.FacebookPageURL;
            mentor.LinkedInURL = updateDto.LinkedInURL;
            mentor.GitHubURL = updateDto.GitHubURL;
            mentor.ProfilePictureURL = updateDto.ProfilePictureURL;

            // Update Mentor-specific properties
            mentor.ProfessionalTitle = updateDto.ProfessionalTitle;
            mentor.YearsOfMentoringExperience = updateDto.YearsOfMentoringExperience;
            mentor.MentoringStyle = updateDto.MentoringStyle;
            mentor.AreasOfMentorship = updateDto.AreasOfMentorship;
            mentor.Availability = updateDto.Availability;
            mentor.PreferredCommunicationMethod = updateDto.PreferredCommunicationMethod;

            await _dbContext.SaveChangesAsync();
            return mentor;
        }

        public async Task<NonprofitOrganization> UpdateNonprofitProfileAsync(Guid userId, NonprofitProfileUpdateDto updateDto)
        {
            var nonprofit = await GetNonprofitByIdAsync(userId);
            if (nonprofit == null)
                throw new KeyNotFoundException($"Nonprofit with ID {userId} not found");

            // Update base user properties
            nonprofit.FullName = updateDto.FullName;
            nonprofit.Biography = updateDto.Biography;
            nonprofit.FacebookPageURL = updateDto.FacebookPageURL;
            nonprofit.LinkedInURL = updateDto.LinkedInURL;
            nonprofit.GitHubURL = updateDto.GitHubURL;
            nonprofit.ProfilePictureURL = updateDto.ProfilePictureURL;

            // Update Nonprofit-specific properties
            nonprofit.OrganizationType = updateDto.OrganizationType;
            nonprofit.MissionStatement = updateDto.MissionStatement;
            nonprofit.WebsiteURL = updateDto.WebsiteURL;
            nonprofit.Location = updateDto.Location;
            nonprofit.FocusAreas = updateDto.FocusAreas;
            nonprofit.YearFounded = updateDto.YearFounded;
            nonprofit.TeamSize = updateDto.TeamSize;

            await _dbContext.SaveChangesAsync();
            return nonprofit;
        }

        public async Task<IEnumerable<TechExpertise>> GetGainerExpertiseAsync(Guid userId)
        {
            var gainer = await GetGainerByIdAsync(userId);
            return gainer?.TechExpertise ?? Enumerable.Empty<TechExpertise>();
        }

        public async Task<IEnumerable<TechExpertise>> GetMentorExpertiseAsync(Guid userId)
        {
            var mentor = await GetMentorByIdAsync(userId);
            return mentor?.TechExpertise ?? Enumerable.Empty<TechExpertise>();
        }

        public async Task<IEnumerable<NonprofitExpertise>> GetNonprofitExpertiseAsync(Guid userId)
        {
            var nonprofit = await GetNonprofitByIdAsync(userId);
            return nonprofit?.NonprofitExpertise ?? Enumerable.Empty<NonprofitExpertise>();
        }

        public async Task<TechExpertise> AddExpertiseToGainerAsync(Guid userId, TechExpertise expertise)
        {
            var gainer = await GetGainerByIdAsync(userId);
            if (gainer == null)
                throw new KeyNotFoundException($"Gainer with ID {userId} not found");

            gainer.TechExpertise.Add(expertise);
            await _dbContext.SaveChangesAsync();
            return expertise;
        }

        public async Task<TechExpertise> AddExpertiseToMentorAsync(Guid userId, TechExpertise expertise)
        {
            var mentor = await GetMentorByIdAsync(userId);
            if (mentor == null)
                throw new KeyNotFoundException($"Mentor with ID {userId} not found");

            mentor.TechExpertise.Add(expertise);
            await _dbContext.SaveChangesAsync();
            return expertise;
        }

        public async Task<NonprofitExpertise> AddExpertiseToNonprofitAsync(Guid userId, NonprofitExpertise expertise)
        {
            var nonprofit = await GetNonprofitByIdAsync(userId);
            if (nonprofit == null)
                throw new KeyNotFoundException($"Nonprofit with ID {userId} not found");

            nonprofit.NonprofitExpertise.Add(expertise);
            await _dbContext.SaveChangesAsync();
            return expertise;
        }

        public async Task<IEnumerable<UserAchievement>> GetGainerAchievementsAsync(Guid userId)
        {
            var gainer = await GetGainerByIdAsync(userId);
            return gainer?.Achievements ?? Enumerable.Empty<UserAchievement>();
        }

        public async Task<IEnumerable<UserAchievement>> GetMentorAchievementsAsync(Guid userId)
        {
            var mentor = await GetMentorByIdAsync(userId);
            return mentor?.Achievements ?? Enumerable.Empty<UserAchievement>();
        }

        public async Task<IEnumerable<UserAchievement>> GetNonprofitAchievementsAsync(Guid userId)
        {
            var nonprofit = await GetNonprofitByIdAsync(userId);
            return nonprofit?.Achievements ?? Enumerable.Empty<UserAchievement>();
        }

        public async Task<UserAchievement> AddAchievementToGainerAsync(Guid userId, Guid achievementTemplateId)
        {
            var gainer = await GetGainerByIdAsync(userId);
            if (gainer == null)
                throw new KeyNotFoundException($"Gainer with ID {userId} not found");

            var achievementTemplate = await _dbContext.AchievementTemplates.FindAsync(achievementTemplateId);
            if (achievementTemplate == null)
                throw new KeyNotFoundException($"Achievement template with ID {achievementTemplateId} not found");

            var achievement = new UserAchievement
            {
                UserId = userId,
                AchievementTemplateId = achievementTemplateId,
                EarnedDate = DateTime.UtcNow
            };

            gainer.Achievements.Add(achievement);
            await _dbContext.SaveChangesAsync();
            return achievement;
        }

        public async Task<UserAchievement> AddAchievementToMentorAsync(Guid userId, Guid achievementTemplateId)
        {
            var mentor = await GetMentorByIdAsync(userId);
            if (mentor == null)
                throw new KeyNotFoundException($"Mentor with ID {userId} not found");

            var achievementTemplate = await _dbContext.AchievementTemplates.FindAsync(achievementTemplateId);
            if (achievementTemplate == null)
                throw new KeyNotFoundException($"Achievement template with ID {achievementTemplateId} not found");

            var achievement = new UserAchievement
            {
                UserId = userId,
                AchievementTemplateId = achievementTemplateId,
                EarnedDate = DateTime.UtcNow
            };

            mentor.Achievements.Add(achievement);
            await _dbContext.SaveChangesAsync();
            return achievement;
        }

        public async Task<UserAchievement> AddAchievementToNonprofitAsync(Guid userId, Guid achievementTemplateId)
        {
            var nonprofit = await GetNonprofitByIdAsync(userId);
            if (nonprofit == null)
                throw new KeyNotFoundException($"Nonprofit with ID {userId} not found");

            var achievementTemplate = await _dbContext.AchievementTemplates.FindAsync(achievementTemplateId);
            if (achievementTemplate == null)
                throw new KeyNotFoundException($"Achievement template with ID {achievementTemplateId} not found");

            var achievement = new UserAchievement
            {
                UserId = userId,
                AchievementTemplateId = achievementTemplateId,
                EarnedDate = DateTime.UtcNow
            };

            nonprofit.Achievements.Add(achievement);
            await _dbContext.SaveChangesAsync();
            return achievement;
        }

        public async Task<IEnumerable<UserProject>> GetGainerProjectHistoryAsync(Guid userId)
        {
            var gainer = await GetGainerByIdAsync(userId);
            return gainer?.ParticipatedProjects ?? Enumerable.Empty<UserProject>();
        }

        public async Task<IEnumerable<UserProject>> GetMentorProjectHistoryAsync(Guid userId)
        {
            var mentor = await GetMentorByIdAsync(userId);
            return mentor?.MentoredProjects ?? Enumerable.Empty<UserProject>();
        }

        public async Task<IEnumerable<UserProject>> GetNonprofitProjectHistoryAsync(Guid userId)
        {
            var nonprofit = await GetNonprofitByIdAsync(userId);
            return nonprofit?.OwnedProjects ?? Enumerable.Empty<UserProject>();
        }

        public async Task<IEnumerable<Gainer>> SearchGainersAsync(string searchTerm)
        {
            return await _dbContext.Gainers
                .Include(g => g.TechExpertise)
                .Where(g => g.FullName.Contains(searchTerm) || 
                           g.Biography.Contains(searchTerm) ||
                           g.TechExpertise.Any(e => e.Name.Contains(searchTerm)))
                .ToListAsync();
        }

        public async Task<IEnumerable<Mentor>> SearchMentorsAsync(string searchTerm)
        {
            return await _dbContext.Mentors
                .Include(m => m.TechExpertise)
                .Where(m => m.FullName.Contains(searchTerm) || 
                           m.Biography.Contains(searchTerm) ||
                           m.TechExpertise.Any(e => e.Name.Contains(searchTerm)))
                .ToListAsync();
        }

        public async Task<IEnumerable<NonprofitOrganization>> SearchNonprofitsAsync(string searchTerm)
        {
            return await _dbContext.Nonprofits
                .Include(n => n.NonprofitExpertise)
                .Where(n => n.FullName.Contains(searchTerm) || 
                           n.Biography.Contains(searchTerm) ||
                           n.NonprofitExpertise.Any(e => e.Name.Contains(searchTerm)))
                .ToListAsync();
        }

        public async Task<GainerStats> GetGainerStatsAsync(Guid userId)
        {
            var gainer = await GetGainerByIdAsync(userId);
            if (gainer == null)
                throw new KeyNotFoundException($"Gainer with ID {userId} not found");

            return new GainerStats
            {
                TotalProjects = gainer.ParticipatedProjects.Count,
                CompletedProjects = gainer.ParticipatedProjects.Count(p => p.ProjectStatus == eProjectStatus.Completed),
                TotalAchievements = gainer.Achievements.Count,
                ExpertiseCount = gainer.TechExpertise.Count
            };
        }

        public async Task<MentorStats> GetMentorStatsAsync(Guid userId)
        {
            var mentor = await GetMentorByIdAsync(userId);
            if (mentor == null)
                throw new KeyNotFoundException($"Mentor with ID {userId} not found");

            return new MentorStats
            {
                TotalMentoredProjects = mentor.MentoredProjects.Count,
                CompletedMentoredProjects = mentor.MentoredProjects.Count(p => p.ProjectStatus == eProjectStatus.Completed),
                TotalAchievements = mentor.Achievements.Count,
                ExpertiseCount = mentor.TechExpertise.Count
            };
        }

        public async Task<NonprofitStats> GetNonprofitStatsAsync(Guid userId)
        {
            var nonprofit = await GetNonprofitByIdAsync(userId);
            if (nonprofit == null)
                throw new KeyNotFoundException($"Nonprofit with ID {userId} not found");

            return new NonprofitStats
            {
                TotalOwnedProjects = nonprofit.OwnedProjects.Count,
                CompletedProjects = nonprofit.OwnedProjects.Count(p => p.ProjectStatus == eProjectStatus.Completed),
                TotalAchievements = nonprofit.Achievements.Count,
                ExpertiseCount = nonprofit.NonprofitExpertise.Count
            };
        }
    }
}
