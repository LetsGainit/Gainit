using GainIt.API.DTOs.ViewModels.Achievement;
using GainIt.API.DTOs.ViewModels.Expertise;
using GainIt.API.DTOs.ViewModels.Projects;
using GainIt.API.Models.Users.Gainers;
using GainIt.API.Models.Projects;
using GainIt.API.Models.Users;

namespace GainIt.API.DTOs.ViewModels.Users
{
    public class FullGainerViewModel : BaseFullUserViewModel
    {
        public string EducationStatus { get; set; }
        public List<string> AreasOfInterest { get; set; } = new List<string>();
        public TechExpertiseViewModel TechExpertise { get; set; }
        public List<ConciseUserProjectViewModel> ParticipatedProjects { get; set; } = new List<ConciseUserProjectViewModel>();
        public List<AchievementViewModel> Achievements { get; set; } = new List<AchievementViewModel>();

        public FullGainerViewModel(Gainer gainer, List<UserProject> projects, List<UserAchievement> achievements) : base(gainer)
        {
            EducationStatus = gainer.EducationStatus;
            AreasOfInterest = gainer.AreasOfInterest;

            TechExpertise = new TechExpertiseViewModel(gainer.TechExpertise);

            ParticipatedProjects = projects
                .Select(participatedProject => new ConciseUserProjectViewModel(participatedProject, gainer.UserId))
                .ToList();

            Achievements = achievements
                .Select(userAchievement => new AchievementViewModel(userAchievement))
                .ToList();
        }
    }
}
