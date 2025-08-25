namespace GainIt.API.DTOs.ViewModels.GitHub
{
    /// <summary>
    /// Response DTO for URL validation
    /// </summary>
    public class UrlValidationResponseDto
    {
        /// <summary>
        /// The validated repository URL
        /// </summary>
        public string RepositoryUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether the URL is valid and accessible
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// Human-readable validation message
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
