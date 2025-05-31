using GainIt.API.Models.Users;

namespace GainIt.API.Models.Projects
{
    public class ProjectUserRole
    {
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }

        public string? UserRole{ get; set; }

        // Navigation properties
        public required UserProject Project { get; set; }
        public required User User { get; set; }
    }
}
