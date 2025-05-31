using GainIt.API.Models.Projects;

namespace GainIt.API.DTOs.ViewModels.Projects
{
    public class TemplateProjectViewModel
    {
        public Guid Id { get; set; }
        public string ProjectName { get; set; }
        public string ProjectDescription { get; set; }
        public string DifficultyLevel { get; set; }
        public string ProjectPictureUrl { get; set; }
        public string Duration { get; set; }
        public string Goals { get; set; }
        public List<string> Technologies { get; set; }

        public TemplateProjectViewModel(TemplateProject i_template)
        {
            Id = i_template.ProjectId;
            ProjectName = i_template.ProjectName;
            ProjectDescription = i_template.ProjectDescription;
            DifficultyLevel = i_template.DifficultyLevel.ToString();
            ProjectPictureUrl = i_template.ProjectPictureUrl;
            Duration = toDaysAndMonthsString(i_template.Duration);
            Goals = i_template.Goals;
            Technologies = i_template.Technologies;
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
