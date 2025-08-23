using System.ComponentModel.DataAnnotations;

namespace GainIt.API.DTOs.Requests.GitHub
{
    /// <summary>
    /// Request DTO for linking a GitHub repository to a project
    /// </summary>
    public class GitHubRepositoryLinkDto
    {
        /// <summary>
        /// The GitHub repository URL (e.g., https://github.com/owner/repo)
        /// </summary>
        /// <example>https://github.com/microsoft/vscode</example>
        [Required(ErrorMessage = "Repository URL is required")]
        [Url(ErrorMessage = "Invalid URL format")]
        [StringLength(2000, ErrorMessage = "Repository URL cannot exceed 2000 characters")]
        public string RepositoryUrl { get; set; } = string.Empty;
    }
}
