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
            // Extract data before JsonIgnore takes effect
            Goals = i_template.Goals.ToList();
            Technologies = i_template.Technologies.ToList();
            RequiredRoles = i_template.RequiredRoles.ToList();
        }

        
    }
}
