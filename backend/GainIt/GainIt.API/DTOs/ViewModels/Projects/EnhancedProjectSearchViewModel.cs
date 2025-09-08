using System.Text.Json.Serialization;

namespace GainIt.API.DTOs.ViewModels.Projects
{
    /// <summary>
    /// Enhanced view model for project search results with additional frontend-required data
    /// </summary>
    public class EnhancedProjectSearchViewModel
    {
        /// <summary>
        /// Unique project identifier
        /// </summary>
        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>
        /// Project name
        /// </summary>
        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// Project description
        /// </summary>
        [JsonPropertyName("projectDescription")]
        public string ProjectDescription { get; set; } = string.Empty;

        /// <summary>
        /// Project picture/thumbnail URL
        /// </summary>
        [JsonPropertyName("projectPictureUrl")]
        public string? ProjectPictureUrl { get; set; }

        /// <summary>
        /// Difficulty level
        /// </summary>
        [JsonPropertyName("difficultyLevel")]
        public string DifficultyLevel { get; set; } = string.Empty;

        /// <summary>
        /// Project duration in days
        /// </summary>
        [JsonPropertyName("durationDays")]
        public int DurationDays { get; set; }

        /// <summary>
        /// Project duration as a human-readable string (e.g., "2-3 weeks", "1 month")
        /// </summary>
        [JsonPropertyName("durationText")]
        public string DurationText { get; set; } = string.Empty;

        /// <summary>
        /// Open roles available for this project
        /// For templates: all required roles
        /// For user projects: roles that still need team members
        /// </summary>
        [JsonPropertyName("openRoles")]
        public string[] OpenRoles { get; set; } = new string[0];

        /// <summary>
        /// All required roles for the project (for templates, this is the same as openRoles)
        /// </summary>
        [JsonPropertyName("requiredRoles")]
        public string[] RequiredRoles { get; set; } = new string[0];

        /// <summary>
        /// Technologies used in the project
        /// </summary>
        [JsonPropertyName("technologies")]
        public string[] Technologies { get; set; } = new string[0];

        /// <summary>
        /// Project goals and objectives
        /// </summary>
        [JsonPropertyName("goals")]
        public string[] Goals { get; set; } = new string[0];

        /// <summary>
        /// Programming languages used in the project (UserProject only)
        /// </summary>
        [JsonPropertyName("programmingLanguages")]
        public string[] ProgrammingLanguages { get; set; } = new string[0];

        /// <summary>
        /// Source of the project (UserProject only)
        /// </summary>
        [JsonPropertyName("projectSource")]
        public string? ProjectSource { get; set; }

        /// <summary>
        /// Current status of the project (UserProject only)
        /// </summary>
        [JsonPropertyName("projectStatus")]
        public string? ProjectStatus { get; set; }

        /// <summary>
        /// Repository link (UserProject only)
        /// </summary>
        [JsonPropertyName("repositoryLink")]
        public string? RepositoryLink { get; set; }


        /// <summary>
        /// Current team size (for user projects)
        /// </summary>
        [JsonPropertyName("teamSize")]
        public int? TeamSize { get; set; }


    }
}
