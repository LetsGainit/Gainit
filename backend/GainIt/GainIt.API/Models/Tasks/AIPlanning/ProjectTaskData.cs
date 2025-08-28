namespace GainIt.API.Models.Tasks.AIPlanning
{
    /// <summary>
    /// Data class for deserializing AI-generated task data from JSON responses
    /// </summary>
    public class ProjectTaskData
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public int MilestoneId { get; set; }
        public string AssignedRole { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public string DueAtUtc { get; set; } = string.Empty;
        public List<ProjectSubtaskData> Subtasks { get; set; } = new();
    }
}
