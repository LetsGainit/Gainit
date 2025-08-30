using GainIt.API.Models.Users;

namespace GainIt.API.Services.Users.Interfaces
{
    public interface IAchievementService
    {
        /// <summary>
        /// Checks and awards achievements for a user when they complete a project
        /// </summary>
        /// <param name="userId">The user who completed the project</param>
        /// <param name="projectId">The completed project ID</param>
        /// <returns>List of newly awarded achievements</returns>
        Task<IEnumerable<UserAchievement>> CheckAndAwardProjectCompletionAchievementsAsync(Guid userId, Guid projectId);

        /// <summary>
        /// Checks and awards achievements for a user when they join a new project
        /// </summary>
        /// <param name="userId">The user who joined the project</param>
        /// <param name="projectId">The project they joined</param>
        /// <returns>List of newly awarded achievements</returns>
        Task<IEnumerable<UserAchievement>> CheckAndAwardTeamParticipationAchievementsAsync(Guid userId, Guid projectId);

        /// <summary>
        /// Checks if a user qualifies for a specific achievement template
        /// </summary>
        /// <param name="userId">The user to check</param>
        /// <param name="achievementTemplateId">The achievement template to check against</param>
        /// <returns>True if the user qualifies for the achievement</returns>
        Task<bool> CheckAchievementCriteriaAsync(Guid userId, Guid achievementTemplateId);

        /// <summary>
        /// Awards an achievement to a user if they don't already have it
        /// </summary>
        /// <param name="userId">The user to award the achievement to</param>
        /// <param name="achievementTemplateId">The achievement template ID</param>
        /// <param name="earnedDetails">Details about how the achievement was earned</param>
        /// <returns>The awarded achievement, or null if already earned</returns>
        Task<UserAchievement?> AwardAchievementAsync(Guid userId, Guid achievementTemplateId, string earnedDetails);
    }
}