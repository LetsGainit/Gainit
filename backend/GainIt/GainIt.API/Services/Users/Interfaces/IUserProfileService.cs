using GainIt.API.DTOs.Requests.Users;
using GainIt.API.DTOs.ViewModels.Users;
using GainIt.API.DTOs.ViewModels.Users.Stats;
using GainIt.API.Models.Projects;
using GainIt.API.Models.Users;
using GainIt.API.Models.Users.Expertise;
using GainIt.API.Models.Users.Gainers;
using GainIt.API.Models.Users.Mentors;
using GainIt.API.Models.Users.Nonprofits;

namespace GainIt.API.Services.Users.Interfaces
{
    public interface IUserProfileService
    {
        // Get user by ID with type-specific details
        Task<Gainer> GetGainerByIdAsync(Guid userId);
        Task<Mentor> GetMentorByIdAsync(Guid userId);
        Task<NonprofitOrganization> GetNonprofitByIdAsync(Guid userId);
        
        // Update user profiles with type-specific data
        Task<Gainer> UpdateGainerProfileAsync(Guid userId, GainerProfileUpdateDTO updateDto);
        Task<Mentor> UpdateMentorProfileAsync(Guid userId, MentorProfileUpdateDTO updateDto);
        Task<NonprofitOrganization> UpdateNonprofitProfileAsync(Guid userId, NonprofitProfileUpdateDTO updateDto);
        
        // Get expertise with type-specific details
        Task<IEnumerable<TechExpertise>> GetGainerExpertiseAsync(Guid userId);
        Task<IEnumerable<TechExpertise>> GetMentorExpertiseAsync(Guid userId);
        Task<IEnumerable<NonprofitExpertise>> GetNonprofitExpertiseAsync(Guid userId);
        
        // Add expertise with type-specific handling
        Task<TechExpertise> AddExpertiseToGainerAsync(Guid userId, TechExpertise expertise);
        Task<TechExpertise> AddExpertiseToMentorAsync(Guid userId, TechExpertise expertise);
        Task<NonprofitExpertise> AddExpertiseToNonprofitAsync(Guid userId, NonprofitExpertise expertise);
        
        // Get achievements with type-specific context
        Task<IEnumerable<UserAchievement>> GetGainerAchievementsAsync(Guid userId);
        Task<IEnumerable<UserAchievement>> GetMentorAchievementsAsync(Guid userId);
        Task<IEnumerable<UserAchievement>> GetNonprofitAchievementsAsync(Guid userId);
        
        // Add achievements with type-specific handling
        Task<UserAchievement> AddAchievementToGainerAsync(Guid userId, Guid achievementTemplateId);
        Task<UserAchievement> AddAchievementToMentorAsync(Guid userId, Guid achievementTemplateId);
        Task<UserAchievement> AddAchievementToNonprofitAsync(Guid userId, Guid achievementTemplateId);
        
        // Get project history with type-specific context
        Task<IEnumerable<UserProject>> GetGainerProjectHistoryAsync(Guid userId);
        Task<IEnumerable<UserProject>> GetMentorProjectHistoryAsync(Guid userId);
        Task<IEnumerable<UserProject>> GetNonprofitProjectHistoryAsync(Guid userId);
        Task<List<UserProject>> GetUserProjectsAsync(Guid userId);
        
        // Search users with type-specific filtering
        Task<IEnumerable<Gainer>> SearchGainersAsync(string searchTerm);
        Task<IEnumerable<Mentor>> SearchMentorsAsync(string searchTerm);
        Task<IEnumerable<NonprofitOrganization>> SearchNonprofitsAsync(string searchTerm);
        
        // Get user statistics with type-specific metrics


        // we to talk about this feature 

        //Task<GainerStats> GetGainerStatsAsync(Guid userId);
        //Task<MentorStats> GetMentorStatsAsync(Guid userId);
        //Task<NonprofitStats> GetNonprofitStatsAsync(Guid userId);
    }
}
