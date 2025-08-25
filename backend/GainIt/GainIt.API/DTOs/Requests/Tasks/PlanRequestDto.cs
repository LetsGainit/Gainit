using GainIt.API.Models.Enums.Tasks;

namespace GainIt.API.DTOs.Requests.Tasks
{
    public class PlanRequestDto
    {
        public eRoadmapPlanMode Mode { get; set; } = eRoadmapPlanMode.AiPlan;

        public Guid? TemplateProjectId { get; set; } // if copy from template 

        public string? Goal { get; set; }
        public string? Constraints { get; set; }
        public string? PreferredTechnologies { get; set; } 
        public DateTime? StartDateUtc { get; set; } 
        public DateTime? TargetDueDateUtc { get; set; }

        public List<string> TeamRoles { get; set; } = new();
    }
}
