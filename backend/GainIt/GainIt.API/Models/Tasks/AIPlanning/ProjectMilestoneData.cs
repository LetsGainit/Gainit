namespace GainIt.API.Models.Tasks.AIPlanning
{
    /// <summary>
    /// Data class for deserializing AI-generated milestone data from JSON responses
    /// </summary>
    public class ProjectMilestoneData
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public int DaysFromStart { get; set; }
    }
}
