namespace GainIt.API.DTOs.ViewModels.Tasks
{
    public class ProjectSubtaskViewModel
    {
        public Guid SubtaskId { get; set; }
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public bool IsDone { get; set; }
        public int OrderIndex { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
    }
}
