using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Users;

namespace GainIt.API.Models.Projects
{
    public class JoinRequest
    {
        public Guid JoinRequestId { get; set; } = Guid.NewGuid();

        public Guid ProjectId { get; set; }
        public UserProject Project { get; set; } = default!;

        public Guid RequesterUserId { get; set; }
        public User RequesterUser { get; set; } = default!;

        public eJoinRequestStatus Status { get; set; } = eJoinRequestStatus.Pending;

        public string? Message { get; set; }
        public string? DecisionReason { get; set; }

        public Guid? DeciderUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? DecisionAtUtc { get; set; }

        public string RequestedRole { get; set; }
    }
}
