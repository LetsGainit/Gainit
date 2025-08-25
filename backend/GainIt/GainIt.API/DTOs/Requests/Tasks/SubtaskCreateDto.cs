namespace GainIt.API.DTOs.Requests.Tasks
{
    public class SubtaskCreateDto
    {
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public int? OrderIndex { get; set; }
    }
}
