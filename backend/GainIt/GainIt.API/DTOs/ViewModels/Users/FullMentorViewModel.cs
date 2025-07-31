using GainIt.API.DTOs.ViewModels.Achievement;
using GainIt.API.DTOs.ViewModels.Expertise;
using GainIt.API.DTOs.ViewModels.Projects;
using GainIt.API.Models.Projects;
using GainIt.API.Models.Users;
using GainIt.API.Models.Users.Mentors;

namespace GainIt.API.DTOs.ViewModels.Users
{
    public class FullMentorViewModel : BaseFullUserViewModel
    {
        public int YearsOfExperience { get; set; }
        public string AreaOfExpertise { get; set; }
        public TechExpertiseViewModel TechExpertise { get; set; }
        public List<ConciseUserProjectViewModel> MentoredProjects { get; set; } = new List<ConciseUserProjectViewModel>();
        public List<AchievementViewModel> Achievements { get; set; } = new List<AchievementViewModel>();    

        public FullMentorViewModel(Mentor mentor, List<UserProject> projects, List<UserAchievement> achievements) : base(mentor)
        {
            YearsOfExperience = mentor.YearsOfExperience;
            AreaOfExpertise = mentor.AreaOfExpertise;

            TechExpertise = new TechExpertiseViewModel(mentor.TechExpertise);

            MentoredProjects = projects
                .Select(up => new ConciseUserProjectViewModel(up, mentor.UserId))
                .ToList();

            Achievements = achievements
                .Select(ua => new AchievementViewModel(ua))
                .ToList();
        }
    }
}
