using GainIt.API.DTOs.ViewModels.Projects;

namespace GainIt.API.DTOs.Search
{
    public class ProjectMatchResultDto
    {
        public IEnumerable<AzureVectorSearchProjectViewModel> Projects { get; }
        public string Explanation { get; }

        public ProjectMatchResultDto(
            IEnumerable<AzureVectorSearchProjectViewModel> projects,
            string explanation)
        {
            Projects = projects;
            Explanation = explanation;
        }
    }
}
