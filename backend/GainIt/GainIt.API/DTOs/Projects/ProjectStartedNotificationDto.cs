namespace GainIt.API.DTOs.Projects
{
    public class ProjectStartedNotificationDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectDescription { get; set; } = string.Empty;
        public string StartedByUserName { get; set; } = string.Empty;
        public Guid StartedByUserId { get; set; }
        public DateTime StartedAtUtc { get; set; }
        public int TeamMembersCount { get; set; }
        public List<string> Technologies { get; set; } = new List<string>();
    }
}
