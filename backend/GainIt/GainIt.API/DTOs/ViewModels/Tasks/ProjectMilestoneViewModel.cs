using GainIt.API.Models.Enums.Tasks;

namespace GainIt.API.DTOs.ViewModels.Tasks
{
    public class ProjectMilestoneViewModel
    {
        public Guid MilestoneId { get; set; }
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public eMilestoneStatus Status { get; set; }
        public int TasksCount { get; set; }
        public int DoneTasksCount { get; set; }
        public int OrderIndex { get; set; }
        public DateTime? TargetDateUtc { get; set; }
    }
}
