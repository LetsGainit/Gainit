namespace GainIt.API.DTOs.Requests.Projects
{
    public class JoinRequestSummaryDto
    {
        public Guid JoinRequestId { get; set; }
        public Guid ProjectId { get; set; }

        public Guid RequesterUserId { get; set; }
        public string RequesterFullName { get; set; } = string.Empty;
        public string RequesterEmailAddress { get; set; } = string.Empty;

        public string? Message { get; set; }
        public string Status { get; set; } = string.Empty;

        public string? DecisionReason { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? DecisionAtUtc { get; set; }
    }
}
