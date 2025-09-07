namespace GainIt.API.DTOs.ViewModels.Projects
{
    public class ProjectMatchResultViewModel
    {
        public List<AzureVectorSearchProjectViewModel> Projects { get; }
        public string Explanation { get; }

        public ProjectMatchResultViewModel(
            IEnumerable<AzureVectorSearchProjectViewModel> projectVms,
            string explanation)
        {
            Projects = projectVms.ToList();
            Explanation = explanation;
        }
    }
}
