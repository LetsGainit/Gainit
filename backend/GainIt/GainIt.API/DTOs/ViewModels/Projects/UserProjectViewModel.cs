using GainIt.API.DTOs.ViewModels.Users;
using GainIt.API.Models.Projects;
using GainIt.API.Models.Users;

namespace GainIt.API.DTOs.ViewModels.Projects
{
    public class UserProjectViewModel
    {
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectDescription { get; set; }
        public string ProjectStatus { get; set; }
        public string DifficultyLevel { get; set; }
        public string ProjectSource { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public List<ConciseUserViewModel> ProjectTeamMembers { get; set; } = new List<ConciseUserViewModel>();
        public string? RepositoryLink { get; set; }
        public FullNonprofitViewModel? OwningOrganization { get; set; }
        public FullMentorViewModel? AssignedMentor { get; set; }
        public string? ProjectPictureUrl { get; set; }
        public int? Duration { get; set; }
        public string? DurationText { get; set; }
        public List<string> OpenRoles { get; set; } = new List<string>();
        public List<string> ProgrammingLanguages { get; set; } = new List<string>();
        public List<string> Goals { get; set; } = new List<string>();
        public List<string> Technologies { get; set; } = new List<string>();

        public UserProjectViewModel(UserProject i_Project)
        {
            ProjectId = i_Project.ProjectId.ToString();
            ProjectName = i_Project.ProjectName;
            ProjectDescription = i_Project.ProjectDescription;
            ProjectStatus = i_Project.ProjectStatus.ToString();
            DifficultyLevel = i_Project.DifficultyLevel.ToString();
            ProjectSource = i_Project.ProjectSource.ToString();
            CreatedAtUtc = i_Project.CreatedAtUtc;
            RepositoryLink = i_Project.RepositoryLink;

            // Extract team member data before JsonIgnore takes effect
            ProjectTeamMembers = i_Project.ProjectMembers
                .Select(member => new ConciseUserViewModel(member))
                .ToList();

            // Extract owning organization data before JsonIgnore takes effect
            OwningOrganization = i_Project.OwningOrganization != null
                ? new FullNonprofitViewModel(i_Project.OwningOrganization, null, false)
                : null;

            ProjectPictureUrl = i_Project.ProjectPictureUrl;
            Duration = (int)Math.Round(i_Project.Duration.TotalDays);
            DurationText = Duration.HasValue ? HumanizeDays(Duration.Value) : null;
            
            // Extract collection data before JsonIgnore takes effect
            OpenRoles = i_Project.RequiredRoles.ToList();
            ProgrammingLanguages = i_Project.ProgrammingLanguages.ToList();
            Goals = i_Project.Goals.ToList();
            Technologies = i_Project.Technologies.ToList();
        }

        private static string HumanizeDays(int days)
        {
            const int daysInYear = 365;
            const int daysInMonth = 30;
            const int daysInWeek = 7;

            if (days <= 0)
            {
                return "0 days";
            }

            if (days % daysInYear == 0)
            {
                int years = days / daysInYear;
                return years == 1 ? "1 year" : $"{years} years";
            }

            if (days % daysInMonth == 0)
            {
                int months = days / daysInMonth;
                return months == 1 ? "1 month" : $"{months} months";
            }

            if (days % daysInWeek == 0)
            {
                int weeks = days / daysInWeek;
                return weeks == 1 ? "1 week" : $"{weeks} weeks";
            }

            return days == 1 ? "1 day" : $"{days} days";
        }
    }
}
