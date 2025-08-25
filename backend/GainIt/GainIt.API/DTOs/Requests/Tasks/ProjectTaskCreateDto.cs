using GainIt.API.Models.Enums.Tasks;
using System.ComponentModel.DataAnnotations;

namespace GainIt.API.DTOs.Requests.Tasks
{
    public class ProjectTaskCreateDto
    {
        [Required, StringLength(120)]
        public string Title { get; set; } = default!;
        [StringLength(4000)]
        public string? Description { get; set; }
        public eTaskType Type { get; set; } = eTaskType.Feature;
        public eTaskPriority Priority { get; set; } = eTaskPriority.Medium;
        public DateTime? DueAtUtc { get; set; }
        public Guid? MilestoneId { get; set; }

        [StringLength(100)]
        public string? AssignedRole { get; set; }   
        public Guid? AssignedUserId { get; set; }  

        public int? OrderIndex { get; set; }        // optional initial order
    }
}
