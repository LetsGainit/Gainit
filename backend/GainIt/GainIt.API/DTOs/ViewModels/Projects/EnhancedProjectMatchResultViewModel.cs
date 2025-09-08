namespace GainIt.API.DTOs.ViewModels.Projects
{
    /// <summary>
    /// Enhanced view model for project search results with additional frontend-required data
    /// </summary>
    public class EnhancedProjectMatchResultViewModel
    {
        public List<EnhancedProjectSearchViewModel> Projects { get; }
        public string Explanation { get; }

        public EnhancedProjectMatchResultViewModel(
            IEnumerable<EnhancedProjectSearchViewModel> projectVms,
            string explanation)
        {
            Projects = projectVms.ToList();
            Explanation = explanation;
        }
    }
}
