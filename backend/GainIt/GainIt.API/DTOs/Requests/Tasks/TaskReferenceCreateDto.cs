using GainIt.API.Models.Enums.Tasks;
using System.ComponentModel.DataAnnotations;

namespace GainIt.API.DTOs.Requests.Tasks
{
    public class TaskReferenceCreateDto
    {
        public eTaskReferenceType Type { get; set; } = eTaskReferenceType.Doc;

        [Required, Url, StringLength(2048)]
        public string Url { get; set; } = default!;

        [StringLength(200)]
        public string? Title { get; set; }
    }
}
