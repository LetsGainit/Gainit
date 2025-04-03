using GainIt.API.Models.Enums.Projects;

namespace GainIt.API.Models.Projects
{
    public class Project
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectDescription { get; set; }
        public eProjectStatus ProjectStatus { get; set; } // "Pending" , "In Progress", "Completed"
        public eDifficultyLevel DifficultyLevel { get; set; }
        public List<string> TechnologyStack { get; set; } = new();
        public eProjectSource ProjectSource { get; set; } // If the project is from a NonprofitOrganization or a built-in project
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public List<Guid> TeamMemberIds { get; set; } = new(); // IDs of users (Gainers)
        public string? RepositoryLink { get; set; }
        public Guid? AssignedMentorId { get; set; }
        public Guid? OwningOrganizationId { get; set; }
    }
}
