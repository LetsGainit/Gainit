namespace GainIt.API.DTOs.Requests.Tasks
{
    public class SubtaskUpdateDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? OrderIndex { get; set; }
        public bool? IsDone { get; set; } 
    }
}
