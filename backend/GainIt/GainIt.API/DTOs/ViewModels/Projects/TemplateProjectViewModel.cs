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
        public string Duration { get; set; }
        public string Goals { get; set; }
        public List<string> Technologies { get; set; }
        public List<string> OpenRoles { get; set; } = new();

        public TemplateProjectViewModel(TemplateProject i_template)
        {
            ProjectId = i_template.ProjectId.ToString();
            ProjectName = i_template.ProjectName;
            ProjectDescription = i_template.ProjectDescription;
            DifficultyLevel = i_template.DifficultyLevel.ToString();
            ProjectPictureUrl = i_template.ProjectPictureUrl;
            Duration = toDaysAndMonthsString(i_template.Duration);
            Goals = i_template.Goals;
            Technologies = i_template.Technologies;
            OpenRoles = i_template.OpenRoles;
        }

        private static string toDaysAndMonthsString(TimeSpan duration)
        {
            int months = (int)(duration.TotalDays / 30);
            int days = (int)(duration.TotalDays % 30);

            if (months > 0)
            {
                return $"{months}m {days}d";
            }

            return $"{days}d";
        }

    }
}
