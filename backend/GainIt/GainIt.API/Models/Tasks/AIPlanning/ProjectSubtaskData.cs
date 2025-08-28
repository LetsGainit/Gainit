namespace GainIt.API.Models.Tasks.AIPlanning
{
    /// <summary>
    /// Data class for deserializing AI-generated subtask data from JSON responses
    /// </summary>
    public class ProjectSubtaskData
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
    }
}
