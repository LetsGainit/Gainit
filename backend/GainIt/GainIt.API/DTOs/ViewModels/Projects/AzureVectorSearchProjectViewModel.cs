using System.Text.Json.Serialization;

namespace GainIt.API.DTOs.ViewModels.Projects
{
    /// <summary>
    /// DTO for exporting projects to Azure Cognitive Search vector indexing
    /// Maps directly to the Azure index schema as shown in the improvement file
    /// </summary>
    public class AzureVectorSearchProjectViewModel
    {
        /// <summary>
        /// Unique project identifier
        /// </summary>
        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>
        /// Project name for exact search and filtering
        /// </summary>
        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// Project description for search
        /// </summary>
        [JsonPropertyName("projectDescription")]
        public string ProjectDescription { get; set; } = string.Empty;

        /// <summary>
        /// Difficulty level for filtering and faceting
        /// </summary>
        [JsonPropertyName("difficultyLevel")]
        public string DifficultyLevel { get; set; } = string.Empty;

        /// <summary>
        /// Project duration in days for easy filtering
        /// </summary>
        [JsonPropertyName("durationDays")]
        public int DurationDays { get; set; }

        /// <summary>
        /// Project goals and objectives - important for project understanding
        /// </summary>
        [JsonPropertyName("goals")]
        public string[] Goals { get; set; } = new string[0];

        /// <summary>
        /// Technologies used in the project
        /// </summary>
        [JsonPropertyName("technologies")]
        public string[] Technologies { get; set; } = new string[0];

        /// <summary>
        /// Required roles for the project
        /// </summary>
        [JsonPropertyName("requiredRoles")]
        public string[] RequiredRoles { get; set; } = new string[0];

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
        public string? ProjectStatus { get; set; 

        /// <summary>
        /// RAG context with all the metadata for search and categorization
        /// This is CRITICAL for vector search in Azure
        /// </summary>
        [JsonPropertyName("ragContext")]
        public RagContextViewModel RagContext { get; set; } = new RagContextViewModel();
    }

    /// <summary>
    /// RAG context metadata for enhanced search capabilities
    /// This structure matches the improvement file exactly
    /// </summary>
    public class RagContextViewModel
    {
        /// <summary>
        /// Rich searchable text for vector search - maps to 'chunk' field in Azure
        /// This is the field that gets vectorized!
        /// </summary>
        [JsonPropertyName("searchableText")]
        public string SearchableText { get; set; } = string.Empty;

        /// <summary>
        /// AI-generated or extracted tags for categorization
        /// </summary>
        [JsonPropertyName("tags")]
        public string[] Tags { get; set; } = new string[0];

        /// <summary>
        /// Required skill levels for the project
        /// </summary>
        [JsonPropertyName("skillLevels")]
        public string[] SkillLevels { get; set; } = new string[0];

        /// <summary>
        /// Type of project (web-app, mobile-app, ai-ml-project, etc.)
        /// </summary>
        [JsonPropertyName("projectType")]
        public string ProjectType { get; set; } = string.Empty;

        /// <summary>
        /// Business domain of the project
        /// </summary>
        [JsonPropertyName("domain")]
        public string Domain { get; set; } = string.Empty;

        /// <summary>
        /// Learning outcomes and skills users will develop
        /// </summary>
        [JsonPropertyName("learningOutcomes")]
        public string[] LearningOutcomes { get; set; } = new string[0];

        /// <summary>
        /// Factors that make this project complex
        /// </summary>
        [JsonPropertyName("complexityFactors")]
        public string[] ComplexityFactors { get; set; } = new string[0];


    }
}
