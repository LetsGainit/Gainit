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

        public eMilestoneStatus Status { get; set; } = eTaskStatus.Todo switch { _ => eMilestoneStatus.Planned };

        [JsonIgnore] public List<ProjectTask> Tasks { get; set; } = new();
    }
}
