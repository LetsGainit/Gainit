namespace GainIt.API.DTOs.Requests.Projects
{
    /// <summary>
    /// Request DTO for creating a project from a template
    /// </summary>
    public class CreateProjectFromTemplateRequestDto
    {
        /// <summary>
        /// The role the user wants to take in the project
        /// Must be one of the roles from the template's RequiredRoles
        /// </summary>
        public string SelectedRole { get; set; } = string.Empty;
    }
}
