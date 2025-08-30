using System.ComponentModel.DataAnnotations;

namespace GainIt.API.DTOs.Requests.Tasks
{
    public class SubtaskUpdateDto
    {
        [StringLength(120)]
        public string? Title { get; set; }
        [StringLength(4000)]
        public string? Description { get; set; }
        public int? OrderIndex { get; set; }
        public bool? IsDone { get; set; } 
    }
}
