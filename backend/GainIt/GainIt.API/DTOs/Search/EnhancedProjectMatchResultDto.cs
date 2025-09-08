using GainIt.API.DTOs.ViewModels.Projects;

namespace GainIt.API.DTOs.Search
{
    public class EnhancedProjectMatchResultDto
    {
        public IEnumerable<EnhancedProjectSearchViewModel> Projects { get; }
        public string Explanation { get; }

        public EnhancedProjectMatchResultDto(
            IEnumerable<EnhancedProjectSearchViewModel> projects,
            string explanation)
        {
            Projects = projects;
            Explanation = explanation;
        }
    }
}
