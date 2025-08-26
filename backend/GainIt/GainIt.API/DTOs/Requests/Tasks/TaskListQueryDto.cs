using GainIt.API.Models.Enums.Tasks;

namespace GainIt.API.DTOs.Requests.Tasks
{
    public class TaskListQueryDto
    {
        public eTaskStatus? Status { get; set; }
        public eTaskType? Type { get; set; }
        public eTaskPriority? Priority { get; set; }
        public Guid? MilestoneId { get; set; }
        public string? AssignedRole { get; set; }
        public Guid? AssignedUserId { get; set; }
        public bool? IsBlocked { get; set; }
        public string? SearchTerm { get; set; }
        public int? Skip { get; set; }
        public int? Take { get; set; } = 50;
        public string? SortBy { get; set; } = "OrderIndex"; // OrderIndex, CreatedAtUtc, DueAtUtc, Priority
        public bool SortDescending { get; set; } = false;
    }
}
