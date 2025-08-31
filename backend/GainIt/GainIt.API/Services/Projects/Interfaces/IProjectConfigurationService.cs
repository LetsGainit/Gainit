namespace GainIt.API.Services.Projects.Interfaces
{
    /// <summary>
    /// Service for managing project configuration and loading projects from JSON files
    /// </summary>
    public interface IProjectConfigurationService
    {
        /// <summary>
        /// Loads all template projects from JSON configuration files
        /// </summary>
        /// <returns>List of template projects ready for seeding</returns>
        List<TemplateProjectDto> LoadTemplateProjects();

        /// <summary>
        /// Loads all nonprofit project suggestions from JSON configuration files
        /// </summary>
        /// <returns>List of nonprofit project suggestions</returns>
        List<NonprofitProjectSuggestion> LoadNonprofitProjectSuggestions();

        /// <summary>
        /// Validates the project configuration files for consistency and completeness
        /// </summary>
        /// <returns>Validation result with any errors or warnings</returns>
        Task<ProjectConfigurationValidationResult> ValidateConfigurationAsync();

        /// <summary>
        /// Gets the last modified timestamp of configuration files for change detection
        /// </summary>
        /// <returns>Last modified timestamp</returns>
        DateTime GetConfigurationLastModified();
    }

    /// <summary>
    /// Data transfer object for template projects loaded from JSON configuration
    /// </summary>
    public class TemplateProjectDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectDescription { get; set; } = string.Empty;
        public string DifficultyLevel { get; set; } = string.Empty;
        public string ProjectPictureUrl { get; set; } = string.Empty;
        public int DurationDays { get; set; }
        public List<string> Goals { get; set; } = new();
        public List<string> Technologies { get; set; } = new();
        public List<string> RequiredRoles { get; set; } = new();
        public List<string> ProgrammingLanguages { get; set; } = new();
    }

    /// <summary>
    /// Data transfer object for nonprofit project suggestions loaded from JSON configuration
    /// </summary>
    public class NonprofitProjectSuggestion
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectDescription { get; set; } = string.Empty;
        public string DifficultyLevel { get; set; } = string.Empty;
        public string ProjectPictureUrl { get; set; } = string.Empty;
        public int DurationDays { get; set; }
        public List<string> Goals { get; set; } = new();
        public List<string> Technologies { get; set; } = new();
        public List<string> RequiredRoles { get; set; } = new();
        public List<string> ProgrammingLanguages { get; set; } = new();
        public string RepositoryLink { get; set; } = string.Empty;
        public string NonprofitName { get; set; } = string.Empty;
        public string NonprofitDescription { get; set; } = string.Empty;
        public string NonprofitWebsiteUrl { get; set; } = string.Empty;
        public string NonprofitProfilePictureUrl { get; set; } = string.Empty;
        public string NonprofitLinkedInUrl { get; set; } = string.Empty;
        public string NonprofitFacebookPageUrl { get; set; } = string.Empty;
        public string NonprofitGitHubUrl { get; set; } = string.Empty;
        public string NonprofitGitHubUsername { get; set; } = string.Empty;
        public string NonprofitCountry { get; set; } = string.Empty;
        public string NonprofitBiography { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of project configuration validation
    /// </summary>
    public class ProjectConfigurationValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public int TotalTemplateProjects { get; set; }
        public int TotalNonprofitSuggestions { get; set; }
    }
}
