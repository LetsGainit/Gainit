using GainIt.API.Models.Enums.Tasks;

namespace GainIt.API.DTOs.ViewModels.Tasks
{
    public class TaskDependencyViewModel
    {
        public Guid TaskId { get; set; }
        public Guid DependsOnTaskId { get; set; }
        public string? DependsOnTitle { get; set; }
        public eTaskStatus DependsOnStatus { get; set; }
    }
}
