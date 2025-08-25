using GainIt.API.Models.Enums.Tasks;
using GainIt.API.Models.Projects;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GainIt.API.Models.Tasks
{
    public class ProjectTask
    {
        [Key] public Guid TaskId { get; set; } = Guid.NewGuid();

        [Required] public Guid ProjectId { get; set; }
        [JsonIgnore] public required UserProject Project { get; set; }

        [Required, StringLength(120)]
        public string Title { get; set; } = default!;

        [StringLength(4000)]
        public string? Description { get; set; }

        public eTaskType Type { get; set; } = eTaskType.Feature;
        public eTaskStatus Status { get; set; } = eTaskStatus.Todo;
        public eTaskPriority Priority { get; set; } = eTaskPriority.Medium;

        public bool IsBlocked { get; set; }
        [StringLength(300)]

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public Guid CreatedByUserId { get; set; }

        public DateTime? DueAtUtc { get; set; }

        public Guid? MilestoneId { get; set; }
        [JsonIgnore] public ProjectMilestone? Milestone { get; set; }

        public int OrderIndex { get; set; }

        [JsonIgnore] public List<ProjectTaskReference> References { get; set; } = new();

        [JsonIgnore] public List<ProjectSubtask> Subtasks { get; set; } = new();

        [JsonIgnore] public List<TaskDependency> Dependencies { get; set; } = new();
        public string? AssignedRole { get; set; } // Role name from ProjectMember.UserRole (e.g., "Backend Developer")
        public Guid? AssignedUserId { get; set; } 
    }
}
