namespace GainIt.API.DTOs.ViewModels.GitHub
{
    /// <summary>
    /// DTO for GitHub repository information
    /// </summary>
    public class GitHubRepositoryInfoDto
    {
        public string RepositoryId { get; set; } = string.Empty;
        public string RepositoryName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPublic { get; set; }
        public string? PrimaryLanguage { get; set; }
        public List<string> Languages { get; set; } = new();
        public int StarsCount { get; set; }
        public int ForksCount { get; set; }
    }
}
