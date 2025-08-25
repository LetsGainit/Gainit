using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GainIt.API.Models.Tasks
{
    public class ProjectSubtask
    {
        [Key] public Guid SubtaskId { get; set; } = Guid.NewGuid();

        [Required] public Guid TaskId { get; set; }
        [JsonIgnore] public required ProjectTask Task { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = default!;

        public string? Description { get; set; }

        public bool IsDone { get; set; }
        public int OrderIndex { get; set; }

        public DateTime? CompletedAtUtc { get; set; }
    }
}
