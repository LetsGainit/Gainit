namespace GainIt.API.DTOs.ViewModels.Tasks
{
    public class TaskElaborationResultViewModel
    {
        public Guid ProjectId { get; set; }
        public Guid TaskId { get; set; }

        public List<string> Notes { get; set; } = new();

    }
}
