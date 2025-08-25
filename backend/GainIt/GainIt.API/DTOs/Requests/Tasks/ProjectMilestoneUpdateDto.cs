using GainIt.API.Models.Enums.Tasks;
using System.ComponentModel.DataAnnotations;

namespace GainIt.API.DTOs.Requests.Tasks
{
    public class ProjectMilestoneUpdateDto
    {
        [StringLength(120)]
        public string? Title { get; set; }
        [StringLength(1000)]
        public string? Description { get; set; }
        public eMilestoneStatus? Status { get; set; }
    }
}
