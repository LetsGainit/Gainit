using GainIt.API.Models.Projects;

namespace GainIt.API.DTOs.ViewModels.Projects
{
    public class JoinRequestViewModel
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
        public bool IsApproved { get; set; } = false;


        public string RequestedRole { get; set; } = string.Empty;

        public string? ProjectName { get; set; }
        public Guid? DeciderUserId { get; set; }

        public JoinRequestViewModel(JoinRequest entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            JoinRequestId = entity.JoinRequestId;
            ProjectId = entity.ProjectId;

            RequesterUserId = entity.RequesterUserId;
            RequesterFullName = entity.RequesterUser?.FullName ?? string.Empty;
            RequesterEmailAddress = entity.RequesterUser?.EmailAddress ?? string.Empty;

            Message = entity.Message;
            Status = entity.Status.ToString();
            DecisionReason = entity.DecisionReason;
            CreatedAtUtc = entity.CreatedAtUtc;
            DecisionAtUtc = entity.DecisionAtUtc;

            RequestedRole = entity.RequestedRole; 

            ProjectName = entity.Project?.ProjectName;
            DeciderUserId = entity.DeciderUserId;
        }
    }
}
