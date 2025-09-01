using System.ComponentModel.DataAnnotations;

namespace GainIt.API.DTOs.Requests.Tasks
{
    public class SubtaskCreateDto
    {
        [Required, StringLength(120)]
        public string Title { get; set; } = default!;
        [StringLength(4000)]
        public string? Description { get; set; }
        public int? OrderIndex { get; set; }
    }
}
