namespace GainIt.API.DTOs.ViewModels.Tasks
{
    public class MilestoneCompletedNotificationDto
    {
        public Guid MilestoneId { get; set; }
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public int TasksCount { get; set; }
        public int DoneTasksCount { get; set; }
        public DateTime CompletedAtUtc { get; set; }
    }
}
