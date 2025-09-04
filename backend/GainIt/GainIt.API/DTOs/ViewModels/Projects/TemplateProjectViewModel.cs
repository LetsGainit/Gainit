using GainIt.API.Models.Projects;

namespace GainIt.API.DTOs.ViewModels.Projects
{
    public class TemplateProjectViewModel 
    {
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectDescription { get; set; }
        public string DifficultyLevel { get; set; }
        public string ProjectPictureUrl { get; set; }
        public int? Duration { get; set; }
        public string? DurationText { get; set; }
        public List<string> Goals { get; set; } = new List<string>();
        public List<string> Technologies { get; set; } = new List<string>();
        public List<string> RequiredRoles { get; set; } = new List<string>();

        public TemplateProjectViewModel(TemplateProject i_template)
        {
            ProjectId = i_template.ProjectId.ToString();
            ProjectName = i_template.ProjectName;
            ProjectDescription = i_template.ProjectDescription;
            DifficultyLevel = i_template.DifficultyLevel.ToString();
            ProjectPictureUrl = i_template.ProjectPictureUrl;
            Duration = (int)Math.Round(i_template.Duration.TotalDays);
            DurationText = Duration.HasValue ? HumanizeDays(Duration.Value) : null;
            // Extract data before JsonIgnore takes effect
            Goals = i_template.Goals.ToList();
            Technologies = i_template.Technologies.ToList();
            RequiredRoles = i_template.RequiredRoles.ToList();
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
