namespace GainIt.API.DTOs.ViewModels.GitHub.Base
{
    /// <summary>
    /// Base DTO for GitHub responses that only contain a message
    /// </summary>
    public class GitHubMessageResponseDto
    {
        /// <summary>
        /// Response message
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
