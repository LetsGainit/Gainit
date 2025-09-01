using GainIt.API.Models.Enums.Tasks;
using GainIt.API.Models.Projects;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GainIt.API.Models.Tasks
{
    public class ProjectMilestone
    {
        [Key] public Guid MilestoneId { get; set; } = Guid.NewGuid();

        [Required] 
        public Guid ProjectId { get; set; }

        [JsonIgnore] 
        public required UserProject Project { get; set; }

        [Required, StringLength(120)]
        public string Title { get; set; } = default!;

        [StringLength(1000)]
        public string? Description { get; set; }

        public eMilestoneStatus Status { get; set; } = eMilestoneStatus.Planned;

        public int OrderIndex { get; set; }

        public DateTime? TargetDateUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public Guid CreatedByUserId { get; set; }

        [JsonIgnore] public List<ProjectTask> Tasks { get; set; } = new();
    }
}
