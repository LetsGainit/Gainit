using GainIt.API.Models.Projects;

namespace GainIt.API.DTOs.ViewModels.Projects
{
    public class ConciseUserProjectViewModel
    {
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectDescription { get; set; }
        public List<string> Technologies { get; set; } = new List<string>();
        public string ProjectStatus { get; set; }
        public string MyRoleInProject { get; set; } 
        public List<string> TeamMembersPictureUrls { get; set; } = new List<string>();
        public string ProjectPictureUrl { get; set; }
        public int? Duration { get; set; }
        public string? DurationText { get; set; }
        public List<string> OpenRoles { get; set; } = new List<string>();

        public ConciseUserProjectViewModel(UserProject i_Project, Guid? i_TeamMemberId)
        {
            ProjectId = i_Project.ProjectId.ToString();
            ProjectName = i_Project.ProjectName;
            ProjectDescription = i_Project.ProjectDescription;
            ProjectPictureUrl = i_Project.ProjectPictureUrl;
            
            // Extract data before JsonIgnore takes effect
            Technologies = i_Project.Technologies.ToList();
            ProjectStatus = i_Project.ProjectStatus.ToString();

            // Duration properties
            Duration = (int)Math.Round(i_Project.Duration.TotalDays);
            DurationText = Duration.HasValue ? HumanizeDays(Duration.Value) : null;

            // Extract open roles before JsonIgnore takes effect
            OpenRoles = i_Project.RequiredRoles.ToList();

            if (i_TeamMemberId.HasValue && i_Project.RoleToIdMap.Count > 0)
            {
                string role = i_Project.RoleToIdMap
                .Where(roleToIdEntry => roleToIdEntry.Value == i_TeamMemberId.Value)
                .Select(roleToIdEntry => roleToIdEntry.Key)
                .FirstOrDefault() ??
                 string.Empty;

                MyRoleInProject = role;
            }
            else
            {
                MyRoleInProject = string.Empty;
            }

            // Extract team member data before JsonIgnore takes effect
            TeamMembersPictureUrls = i_Project.ProjectMembers?
                   .Select(member => member.User.ProfilePictureURL ?? string.Empty)
                   .Where(url => !string.IsNullOrEmpty(url))
                   .ToList() ?? new List<string>();
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
