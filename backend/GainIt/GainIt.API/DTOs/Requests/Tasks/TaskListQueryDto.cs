using GainIt.API.Models.Enums.Tasks;

namespace GainIt.API.DTOs.Requests.Tasks
{
    public class TaskListQueryDto
    {
        public eTaskType? Type { get; set; }
        public eTaskPriority? Priority { get; set; }
        public Guid? MilestoneId { get; set; }
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; } = "OrderIndex"; // OrderIndex, CreatedAtUtc, DueAtUtc, Priority
        public bool SortDescending { get; set; } = false;
    }
}
