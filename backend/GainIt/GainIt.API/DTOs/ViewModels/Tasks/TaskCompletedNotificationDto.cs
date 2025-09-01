namespace GainIt.API.DTOs.ViewModels.Tasks
{
    public class TaskCompletedNotificationDto
    {
        public Guid TaskId { get; set; }
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? AssignedRole { get; set; }
        public Guid? AssignedUserId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public DateTime CompletedAtUtc { get; set; }
    }
}
