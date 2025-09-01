namespace GainIt.API.DTOs.ViewModels.Tasks
{
    public class TaskCreatedNotificationDto
    {
        public Guid TaskId { get; set; }
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string? AssignedRole { get; set; }
        public Guid? AssignedUserId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }
}
