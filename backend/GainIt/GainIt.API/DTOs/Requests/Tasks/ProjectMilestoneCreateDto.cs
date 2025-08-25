using GainIt.API.Models.Enums.Tasks;
using System.ComponentModel.DataAnnotations;

namespace GainIt.API.DTOs.Requests.Tasks
{
    public class ProjectMilestoneCreateDto
    {
        [Required, StringLength(120)]
        public string Title { get; set; } = default!;
        [StringLength(1000)]
        public string? Description { get; set; }
        public eMilestoneStatus Status { get; set; } = eMilestoneStatus.Planned;
    }
}
