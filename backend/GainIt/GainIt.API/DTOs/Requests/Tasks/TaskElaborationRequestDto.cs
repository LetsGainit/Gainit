namespace GainIt.API.DTOs.Requests.Tasks
{
    public class TaskElaborationRequestDto
    {

        public string? UserQuestion { get; set; }  // free text from assignee
        public string? ExtraContext { get; set; }  // repo link, design doc, etc.
    }
}
