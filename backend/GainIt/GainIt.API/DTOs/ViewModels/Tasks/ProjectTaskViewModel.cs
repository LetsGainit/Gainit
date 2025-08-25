namespace GainIt.API.DTOs.ViewModels.Tasks
{
    public class ProjectTaskViewModel : ProjectTaskListItemViewModel
    {
        public string? Description { get; set; }
        public IReadOnlyList<ProjectSubtaskViewModel> Subtasks { get; set; } = Array.Empty<ProjectSubtaskViewModel>();
        public IReadOnlyList<ProjectTaskReferenceViewModel> References { get; set; } = Array.Empty<ProjectTaskReferenceViewModel>();
        public IReadOnlyList<TaskDependencyViewModel> Dependencies { get; set; } = Array.Empty<TaskDependencyViewModel>();
    }
}
