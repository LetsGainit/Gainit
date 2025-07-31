using GainIt.API.Models.Users;
using System.ComponentModel.DataAnnotations;

namespace GainIt.API.DTOs.ViewModels.Achievement
{
    public class AchievementViewModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string IconUrl { get; set; }
        public string UnlockCriteria { get; set; }
        public string Category { get; set; }
        public DateTime DateEarned { get; set; }

        public AchievementViewModel(UserAchievement i_UserAchievement)
        {
            Id = i_UserAchievement.Id.ToString();
            if (i_UserAchievement.AchievementTemplate != null)
            {
                Title = i_UserAchievement.AchievementTemplate.Title;
                Description = i_UserAchievement.AchievementTemplate.Description;
                IconUrl = i_UserAchievement.AchievementTemplate.IconUrl;
                UnlockCriteria = i_UserAchievement.AchievementTemplate.UnlockCriteria;
                Category = i_UserAchievement.AchievementTemplate.Category;
            }
            else
            {
                Title = "Don't hold achievements";
                Description = string.Empty;
                IconUrl = string.Empty;
                UnlockCriteria = string.Empty;
                Category = string.Empty;
            }
            DateEarned = i_UserAchievement.EarnedAtUtc;
        }
    }
}
