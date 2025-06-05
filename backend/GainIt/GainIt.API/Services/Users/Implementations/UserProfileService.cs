using GainIt.API.Data;
using GainIt.API.DTOs.Requests.Users;
using GainIt.API.DTOs.ViewModels.Users;
using GainIt.API.Models.Projects;
using GainIt.API.Models.Users;
using GainIt.API.Models.Users.Expertise;
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

        public UserProfileService(GainItDbContext i_dbContext)
        {
            _dbContext = i_dbContext;
        }

        public async Task<Gainer> GetGainerByIdAsync(Guid i_userId)
        {
            return await _dbContext.Gainers
                .Include(g => g.TechExpertise)
                .Include(g => g.ParticipatedProjects)
                .Include(g => g.Achievements)
                .FirstOrDefaultAsync(g => g.UserId == i_userId) ?? 
                throw new KeyNotFoundException($"Gainer with ID {i_userId} not found");
        }

        public async Task<Mentor> GetMentorByIdAsync(Guid i_userId)
        {
            return await _dbContext.Mentors
                .Include(m => m.TechExpertise)
                .Include(m => m.MentoredProjects)
                .Include(m => m.Achievements)
                .FirstOrDefaultAsync(m => m.UserId == i_userId) ??
                throw new KeyNotFoundException($"Mentor with ID {i_userId} not found");
        }

        public async Task<NonprofitOrganization> GetNonprofitByIdAsync(Guid i_userId)
        {
            return await _dbContext.Nonprofits
                .Include(n => n.NonprofitExpertise)
                .Include(n => n.OwnedProjects)
                .FirstOrDefaultAsync(n => n.UserId == i_userId) ??
                throw new KeyNotFoundException($"Noneprofit with ID {i_userId} not found");
        }

        public async Task<Gainer> UpdateGainerProfileAsync(Guid i_userId, GainerProfileUpdateDTO i_updateDto)
        {
            var gainer = await GetGainerByIdAsync(i_userId);
            if (gainer == null)
                throw new KeyNotFoundException($"Gainer with ID {i_userId} not found");

            // Update base user properties
            gainer.FullName = i_updateDto.FullName;
            gainer.Biography = i_updateDto.Biography;
            gainer.FacebookPageURL = i_updateDto.FacebookPageURL;
            gainer.LinkedInURL = i_updateDto.LinkedInURL;
            gainer.GitHubURL = i_updateDto.GitHubURL;
            gainer.ProfilePictureURL = i_updateDto.ProfilePictureURL;

            // Update Gainer-specific properties
            gainer.EducationStatus = i_updateDto.EducationStatus;
            gainer.AreasOfInterest = i_updateDto.AreasOfInterest;   // we get it as List<string> so we need to think how to take it

            await _dbContext.SaveChangesAsync();
            return gainer;
        }

        public async Task<Mentor> UpdateMentorProfileAsync(Guid userId, MentorProfileUpdateDTO i_updateDto)
        {
            var mentor = await GetMentorByIdAsync(userId);
            if (mentor == null)
                throw new KeyNotFoundException($"Mentor with ID {userId} not found");

            // Update base user properties
            mentor.FullName = i_updateDto.FullName;
            mentor.Biography = i_updateDto.Biography;
            mentor.FacebookPageURL = i_updateDto.FacebookPageURL;
            mentor.LinkedInURL = i_updateDto.LinkedInURL;
            mentor.GitHubURL = i_updateDto.GitHubURL;
            mentor.ProfilePictureURL = i_updateDto.ProfilePictureURL;

            // Update Mentor-specific properties
            mentor.YearsOfExperience = i_updateDto.YearsOfExperience;
            mentor.AreaOfExpertise = i_updateDto.AreaOfExpertise;

            // Update TechExpertise
            if (mentor.TechExpertise == null)
            {
                mentor.TechExpertise = new TechExpertise
                {
                    User = mentor // Set the required 'User' property to the current mentor instance
                };
            }
            mentor.TechExpertise.ProgrammingLanguages = i_updateDto.TechExpertise.ProgrammingLanguages;
            mentor.TechExpertise.Technologies = i_updateDto.TechExpertise.Technologies;
            mentor.TechExpertise.Tools = i_updateDto.TechExpertise.Tools;

            await _dbContext.SaveChangesAsync();
            return mentor;
        }

        public async Task<NonprofitOrganization> UpdateNonprofitProfileAsync(Guid userId, NonprofitProfileUpdateDTO updateDto)
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
            nonprofit.WebsiteUrl = updateDto.WebsiteUrl;

            // Update NonprofitExpertise
            if (nonprofit.NonprofitExpertise == null)
            {
                nonprofit.NonprofitExpertise = new NonprofitExpertise
                {
                    User = nonprofit, 
                    FieldOfWork = updateDto.NonprofitExpertise.FieldOfWork, 
                    MissionStatement = updateDto.NonprofitExpertise.MissionStatement 
                };
            }
            else
            {
                nonprofit.NonprofitExpertise.FieldOfWork = updateDto.NonprofitExpertise.FieldOfWork;
                nonprofit.NonprofitExpertise.MissionStatement = updateDto.NonprofitExpertise.MissionStatement;
            }

            await _dbContext.SaveChangesAsync();
            return nonprofit;
        }

        public async Task<IEnumerable<TechExpertise>> GetGainerExpertiseAsync(Guid userId)
        {
            var gainer = await GetGainerByIdAsync(userId);
            return gainer?.TechExpertise != null
                ? new List<TechExpertise> { gainer.TechExpertise }
                : Enumerable.Empty<TechExpertise>();
        }

        public async Task<IEnumerable<TechExpertise>> GetMentorExpertiseAsync(Guid userId)
        {
            var mentor = await GetMentorByIdAsync(userId);
            return mentor?.TechExpertise != null
                ? new List<TechExpertise> { mentor.TechExpertise }
                : Enumerable.Empty<TechExpertise>();
        }

        public async Task<IEnumerable<NonprofitExpertise>> GetNonprofitExpertiseAsync(Guid userId)
        {
            var nonprofit = await GetNonprofitByIdAsync(userId);
            return nonprofit?.NonprofitExpertise != null
                ? new List<NonprofitExpertise> { nonprofit.NonprofitExpertise }
                : Enumerable.Empty<NonprofitExpertise>();
        }

        public async Task<TechExpertise> AddExpertiseToGainerAsync(Guid userId, TechExpertise expertise)
        {
            var gainer = await GetGainerByIdAsync(userId);
            if (gainer == null)
                throw new KeyNotFoundException($"Gainer with ID {userId} not found");

            // Ensure TechExpertise is initialized before adding expertise  
            if (gainer.TechExpertise == null)
            {
                gainer.TechExpertise = new TechExpertise
                {
                    User = gainer, // Set the required 'User' property to the current gainer instance  
                    ProgrammingLanguages = new List<string>(),
                    Technologies = new List<string>(),
                    Tools = new List<string>()
                };
            }

            // Add expertise details to the respective lists  
            gainer.TechExpertise.ProgrammingLanguages.AddRange(expertise.ProgrammingLanguages);
            gainer.TechExpertise.Technologies.AddRange(expertise.Technologies);
            gainer.TechExpertise.Tools.AddRange(expertise.Tools);

            await _dbContext.SaveChangesAsync();
            return expertise;
        }



        // checked the methods above , from here need to go over






        public async Task<TechExpertise> AddExpertiseToMentorAsync(Guid userId, TechExpertise expertise)
        {
            var mentor = await GetMentorByIdAsync(userId);
            if (mentor == null)
                throw new KeyNotFoundException($"Mentor with ID {userId} not found");

            // Ensure TechExpertise is initialized before adding expertise  
            if (mentor.TechExpertise == null)
            {
                mentor.TechExpertise = new TechExpertise
                {
                    User = mentor, // Set the required 'User' property to the current mentor instance  
                    ProgrammingLanguages = new List<string>(),
                    Technologies = new List<string>(),
                    Tools = new List<string>()
                };
            }

            // Add expertise details to the respective lists  
            mentor.TechExpertise.ProgrammingLanguages.AddRange(expertise.ProgrammingLanguages);
            mentor.TechExpertise.Technologies.AddRange(expertise.Technologies);
            mentor.TechExpertise.Tools.AddRange(expertise.Tools);

            await _dbContext.SaveChangesAsync();
            return expertise;
        }

        public async Task<NonprofitExpertise> AddExpertiseToNonprofitAsync(Guid userId, NonprofitExpertise expertise)
        {
            var nonprofit = await GetNonprofitByIdAsync(userId);
            if (nonprofit == null)
                throw new KeyNotFoundException($"Nonprofit with ID {userId} not found");

            // Ensure NonprofitExpertise is initialized before adding expertise
            if (nonprofit.NonprofitExpertise == null)
            {
                nonprofit.NonprofitExpertise = new NonprofitExpertise
                {
                    User = nonprofit,
                    FieldOfWork = expertise.FieldOfWork,
                    MissionStatement = expertise.MissionStatement
                };
            }
            else
            {
                // Update existing NonprofitExpertise properties
                nonprofit.NonprofitExpertise.FieldOfWork = expertise.FieldOfWork;
                nonprofit.NonprofitExpertise.MissionStatement = expertise.MissionStatement;
            }

            await _dbContext.SaveChangesAsync();
            return nonprofit.NonprofitExpertise;
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
                EarnedAtUtc = DateTime.UtcNow
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
                EarnedAtUtc = DateTime.UtcNow,
                User = mentor,
                AchievementTemplate = achievementTemplate
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
                EarnedAtUtc = DateTime.UtcNow
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

        public Task<IEnumerable<Gainer>> SearchGainersAsync(string searchTerm)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Mentor>> SearchMentorsAsync(string searchTerm)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<NonprofitOrganization>> SearchNonprofitsAsync(string searchTerm)
        {
            throw new NotImplementedException();
        }

        /*public async Task<IEnumerable<Gainer>> SearchGainersAsync(string searchTerm)
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
        }*/



        ////////////////////   the stats sevice? ///////////////////////////////////////////////////

        /*public async Task<GainerStats> GetGainerStatsAsync(Guid userId)
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
        }*/
    }
}
