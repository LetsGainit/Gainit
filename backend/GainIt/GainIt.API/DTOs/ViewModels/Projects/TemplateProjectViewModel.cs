using GainIt.API.Models.Projects;

namespace GainIt.API.DTOs.ViewModels.Projects
{
    public class TemplateProjectViewModel
    {
        public Guid TemplateProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectDescription { get; set; }
        public string DifficultyLevel { get; set; }

        public TemplateProjectViewModel(TemplateProject i_Template)
        {
            TemplateProjectId = i_Template.TemplateProjectId;
            ProjectName = i_Template.ProjectName;
            ProjectDescription = i_Template.ProjectDescription;
            DifficultyLevel = i_Template.DifficultyLevel.ToString();
        }
    }

}
