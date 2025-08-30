using GainIt.API.Data;
using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Users;
using GainIt.API.Services.Users.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GainIt.API.Services.Users.Implementations
{
    public class AchievementService : IAchievementService
    {
        private readonly GainItDbContext r_DbContext;
        private readonly ILogger<AchievementService> r_logger;
        private readonly IUserProfileService r_userProfileService;

        public AchievementService(
            GainItDbContext dbContext, 
            ILogger<AchievementService> logger,
            IUserProfileService userProfileService)
        {
            r_DbContext = dbContext;
            r_logger = logger;
            r_userProfileService = userProfileService;
        }

        public async Task<IEnumerable<UserAchievement>> CheckAndAwardProjectCompletionAchievementsAsync(Guid userId, Guid projectId)
        {
            r_logger.LogInformation("Checking project completion achievements for user: UserId={UserId}, ProjectId={ProjectId}", userId, projectId);

            var awardedAchievements = new List<UserAchievement>();

            try
            {
                // Get the "First Project Complete" achievement template
                var firstProjectTemplate = await r_DbContext.AchievementTemplates
                    .FirstOrDefaultAsync(at => at.Title == "First Project Complete");

                if (firstProjectTemplate == null)
                {
                    r_logger.LogWarning("'First Project Complete' achievement template not found");
                    return awardedAchievements;
                }

                // Check if user already has this achievement
                var hasFirstProjectAchievement = await r_DbContext.UserAchievements
                    .AnyAsync(ua => ua.UserId == userId && ua.AchievementTemplateId == firstProjectTemplate.Id);

                if (!hasFirstProjectAchievement)
                {
                    // Check if this is indeed their first completed project
                    var completedProjectsCount = await r_DbContext.Projects
                        .Where(p => p.ProjectMembers.Any(pm => pm.UserId == userId) && 
                                   p.ProjectStatus == eProjectStatus.Completed)
                        .CountAsync();

                    if (completedProjectsCount == 1) // This is their first completed project
                    {
                        var project = await r_DbContext.Projects
                            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

                        var earnedDetails = $"Completed first project: '{project?.ProjectName}' on {DateTime.UtcNow:yyyy-MM-dd}";
                        
                        var achievement = await AwardAchievementAsync(userId, firstProjectTemplate.Id, earnedDetails);
                        if (achievement != null)
                        {
                            awardedAchievements.Add(achievement);
                        }
                    }
                }

                r_logger.LogInformation("Project completion achievement check completed: UserId={UserId}, AwardedCount={Count}", 
                    userId, awardedAchievements.Count);

                return awardedAchievements;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error checking project completion achievements: UserId={UserId}, ProjectId={ProjectId}", 
                    userId, projectId);
                throw;
            }
        }

        public async Task<IEnumerable<UserAchievement>> CheckAndAwardTeamParticipationAchievementsAsync(Guid userId, Guid projectId)
        {
            r_logger.LogInformation("Checking team participation achievements for user: UserId={UserId}, ProjectId={ProjectId}", userId, projectId);

            var awardedAchievements = new List<UserAchievement>();

            try
            {
                // Get the "Team Player" achievement template
                var teamPlayerTemplate = await r_DbContext.AchievementTemplates
                    .FirstOrDefaultAsync(at => at.Title == "Team Player");

                if (teamPlayerTemplate == null)
                {
                    r_logger.LogWarning("'Team Player' achievement template not found");
                    return awardedAchievements;
                }

                // Check if user already has this achievement
                var hasTeamPlayerAchievement = await r_DbContext.UserAchievements
                    .AnyAsync(ua => ua.UserId == userId && ua.AchievementTemplateId == teamPlayerTemplate.Id);

                if (!hasTeamPlayerAchievement)
                {
                    // Count distinct projects the user is a member of
                    var participatedProjectsCount = await r_DbContext.ProjectMembers
                        .Where(pm => pm.UserId == userId)
                        .Select(pm => pm.ProjectId)
                        .Distinct()
                        .CountAsync();

                    if (participatedProjectsCount >= 5) // User has participated in 5 or more projects
                    {
                        var earnedDetails = $"Participated as team member in {participatedProjectsCount} different projects on {DateTime.UtcNow:yyyy-MM-dd}";
                        
                        var achievement = await AwardAchievementAsync(userId, teamPlayerTemplate.Id, earnedDetails);
                        if (achievement != null)
                        {
                            awardedAchievements.Add(achievement);
                        }
                    }
                }

                r_logger.LogInformation("Team participation achievement check completed: UserId={UserId}, AwardedCount={Count}", 
                    userId, awardedAchievements.Count);

                return awardedAchievements;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error checking team participation achievements: UserId={UserId}, ProjectId={ProjectId}", 
                    userId, projectId);
                throw;
            }
        }

        public async Task<bool> CheckAchievementCriteriaAsync(Guid userId, Guid achievementTemplateId)
        {
            r_logger.LogInformation("Checking achievement criteria: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", 
                userId, achievementTemplateId);

            try
            {
                var template = await r_DbContext.AchievementTemplates
                    .FirstOrDefaultAsync(at => at.Id == achievementTemplateId);

                if (template == null)
                {
                    r_logger.LogWarning("Achievement template not found: AchievementTemplateId={AchievementTemplateId}", achievementTemplateId);
                    return false;
                }

                // Check criteria based on the achievement title (this could be made more robust)
                switch (template.Title)
                {
                    case "First Project Complete":
                        var completedProjectsCount = await r_DbContext.Projects
                            .Where(p => p.ProjectMembers.Any(pm => pm.UserId == userId) && 
                                       p.ProjectStatus == eProjectStatus.Completed)
                            .CountAsync();
                        return completedProjectsCount >= 1;

                    case "Team Player":
                        var participatedProjectsCount = await r_DbContext.ProjectMembers
                            .Where(pm => pm.UserId == userId)
                            .Select(pm => pm.ProjectId)
                            .Distinct()
                            .CountAsync();
                        return participatedProjectsCount >= 5;

                    case "Mentor's Choice":
                        // This would require a feedback system - for now return false
                        // In the future, this could check for positive feedback records
                        return false;

                    default:
                        r_logger.LogWarning("Unknown achievement criteria for template: {Title}", template.Title);
                        return false;
                }
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error checking achievement criteria: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", 
                    userId, achievementTemplateId);
                throw;
            }
        }

        public async Task<UserAchievement?> AwardAchievementAsync(Guid userId, Guid achievementTemplateId, string earnedDetails)
        {
            r_logger.LogInformation("Awarding achievement: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", 
                userId, achievementTemplateId);

            try
            {
                // Check if user already has this achievement
                var existingAchievement = await r_DbContext.UserAchievements
                    .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AchievementTemplateId == achievementTemplateId);

                if (existingAchievement != null)
                {
                    r_logger.LogInformation("User already has achievement: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", 
                        userId, achievementTemplateId);
                    return null;
                }

                // Get the achievement template and user
                var achievementTemplate = await r_DbContext.AchievementTemplates
                    .FirstOrDefaultAsync(at => at.Id == achievementTemplateId);
                var user = await r_DbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);

                if (achievementTemplate == null)
                {
                    r_logger.LogWarning("Achievement template not found: AchievementTemplateId={AchievementTemplateId}", achievementTemplateId);
                    throw new KeyNotFoundException($"Achievement template with ID {achievementTemplateId} not found");
                }

                if (user == null)
                {
                    r_logger.LogWarning("User not found: UserId={UserId}", userId);
                    throw new KeyNotFoundException($"User with ID {userId} not found");
                }

                // Create and award the achievement
                var newAchievement = new UserAchievement
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    AchievementTemplateId = achievementTemplateId,
                    EarnedAtUtc = DateTime.UtcNow,
                    EarnedDetails = earnedDetails,
                    User = user,
                    AchievementTemplate = achievementTemplate
                };

                r_DbContext.UserAchievements.Add(newAchievement);
                await r_DbContext.SaveChangesAsync();

                r_logger.LogInformation("Successfully awarded achievement: UserId={UserId}, AchievementTitle={Title}, AchievementId={AchievementId}", 
                    userId, achievementTemplate.Title, newAchievement.Id);

                return newAchievement;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error awarding achievement: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", 
                    userId, achievementTemplateId);
                throw;
            }
        }
    }
}