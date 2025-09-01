using GainIt.API.Models.Enums.Tasks;

namespace GainIt.API.DTOs.Requests.Tasks
{
    public class PlanRequestDto
    {
        public string? Goal { get; set; }

        public string? Constraints { get; set; }

        public string? PreferredTechnologies { get; set; }

        public DateTime? StartDateUtc { get; set; }

        public DateTime? TargetDueDateUtc { get; set; }
    }
}
