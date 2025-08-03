using GainIt.API.Models.Projects;

namespace GainIt.API.DTOs.Search
{
    public class ProjectMatchResultDto
    {
        public IEnumerable<TemplateProject> Projects { get; }
        public string Explanation { get; }

        public ProjectMatchResultDto(
            IEnumerable<TemplateProject> projects,
            string explanation)
        {
            Projects = projects;
            Explanation = explanation;
        }
    }
}
