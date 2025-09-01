using GainIt.API.Models.Enums.Tasks;

namespace GainIt.API.DTOs.Requests.Tasks
{
    public class TaskBoardQueryDto
    {
        public eTaskType? Type { get; set; }
        public eTaskPriority? Priority { get; set; }
        public Guid? MilestoneId { get; set; }
        public string? AssignedRole { get; set; }
        public Guid? AssignedUserId { get; set; }
        public bool? IsBlocked { get; set; }
        public string? SearchTerm { get; set; }
        public bool IncludeCompleted { get; set; } = false; // Whether to include Done tasks in board view
        public string? SortBy { get; set; } = "OrderIndex"; // OrderIndex, CreatedAtUtc, DueAtUtc, Priority
        public bool SortDescending { get; set; } = false;
    }
}
