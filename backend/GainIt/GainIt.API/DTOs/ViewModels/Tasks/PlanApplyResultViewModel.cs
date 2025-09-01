using GainIt.API.DTOs.ViewModels.Tasks;

namespace GainIt.API.DTOs.ViewModels.Tasks
{
    public class PlanApplyResultViewModel
    {
        public Guid ProjectId { get; set; }

        public List<ProjectMilestoneViewModel> CreatedMilestones { get; set; } = new();

        public List<ProjectTaskViewModel> CreatedTasks { get; set; } = new();

        public List<string> Notes { get; set; } = new();
    }
}
