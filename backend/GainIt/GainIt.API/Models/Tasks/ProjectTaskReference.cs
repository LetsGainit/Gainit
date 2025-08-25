using GainIt.API.Models.Enums.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GainIt.API.Models.Tasks
{
    public class ProjectTaskReference
    {
        [Key] public Guid ReferenceId { get; set; } = Guid.NewGuid();

        [Required] public Guid TaskId { get; set; }
        [JsonIgnore] public required ProjectTask Task { get; set; }

        [Required] public eTaskReferenceType Type { get; set; } = eTaskReferenceType.Doc;

        [Required, Url, StringLength(2048)]
        public string Url { get; set; } = default!;

        [StringLength(200)]
        public string? Title { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
