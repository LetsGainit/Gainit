using GainIt.API.Models.Enums.Tasks;

namespace GainIt.API.DTOs.ViewModels.Tasks
{
    public class ProjectTaskListItemViewModel
    {
        public Guid TaskId { get; set; }
        public string Title { get; set; } = default!;
        public eTaskStatus Status { get; set; }
        public eTaskPriority Priority { get; set; }
        public eTaskType Type { get; set; }
        public bool IsBlocked { get; set; }
        public int OrderIndex { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? DueAtUtc { get; set; }

        public string? AssignedRole { get; set; }
        public Guid? AssignedUserId { get; set; }
        public Guid? MilestoneId { get; set; }
        public string? MilestoneTitle { get; set; }
    }
}
