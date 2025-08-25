using GainIt.API.Models.Enums.Tasks;

namespace GainIt.API.DTOs.ViewModels.Tasks
{
    public class ProjectTaskReferenceViewModel
    {
        public Guid ReferenceId { get; set; }
        public eTaskReferenceType Type { get; set; }
        public string Url { get; set; } = default!;
        public string? Title { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
