using System.ComponentModel.DataAnnotations;

namespace GainIt.API.DTOs.Requests.GitHub
{
    /// <summary>
    /// Request DTO for validating a GitHub repository URL
    /// </summary>
    public class GitHubUrlValidationDto
    {
        /// <summary>
        /// The GitHub repository URL to validate
        /// </summary>
        /// <example>https://github.com/owner/repo</example>
        [Required(ErrorMessage = "Repository URL is required")]
        [Url(ErrorMessage = "Invalid URL format")]
        [StringLength(2000, ErrorMessage = "Repository URL cannot exceed 2000 characters")]
        public string RepositoryUrl { get; set; } = string.Empty;
    }
}
