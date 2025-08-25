using GainIt.API.Models.Enums.Tasks;
using System.ComponentModel.DataAnnotations;

namespace GainIt.API.DTOs.Requests.Tasks
{
    public class ProjectTaskUpdateDto
    {
        [StringLength(120)]
        public string? Title { get; set; }
        [StringLength(4000)]
        public string? Description { get; set; }
        public eTaskType? Type { get; set; }
        public eTaskPriority? Priority { get; set; }
        public DateTime? DueAtUtc { get; set; }
        public Guid? MilestoneId { get; set; }
        [StringLength(30)]
        public string? AssignedRole { get; set; }  
        public Guid? AssignedUserId { get; set; }  
    }
}
