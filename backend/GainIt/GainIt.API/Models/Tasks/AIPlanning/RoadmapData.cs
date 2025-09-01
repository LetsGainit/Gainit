namespace GainIt.API.Models.Tasks.AIPlanning
{
    /// <summary>
    /// Data class for deserializing AI-generated roadmap data from JSON responses
    /// </summary>
    public class RoadmapData
    {
        public List<ProjectMilestoneData> Milestones { get; set; } = new();
        public List<ProjectTaskData> Tasks { get; set; } = new();
    }
}
