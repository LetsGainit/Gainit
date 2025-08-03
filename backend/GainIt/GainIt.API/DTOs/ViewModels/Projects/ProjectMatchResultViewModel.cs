namespace GainIt.API.DTOs.ViewModels.Projects
{
    public class ProjectMatchResultViewModel
    {
        public List<TemplateProjectViewModel> Projects { get; }
        public string Explanation { get; }

        public ProjectMatchResultViewModel(
            IEnumerable<TemplateProjectViewModel> projectVms,
            string explanation)
        {
            Projects = projectVms.ToList();
            Explanation = explanation;
        }
    }
}
