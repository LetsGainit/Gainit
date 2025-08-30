namespace GainIt.API.DTOs.ViewModels.Tasks
{
    public class TaskUnblockedNotificationDto
    {
        public Guid TaskId { get; set; }
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? AssignedRole { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public DateTime UnblockedAtUtc { get; set; }
    }
}
