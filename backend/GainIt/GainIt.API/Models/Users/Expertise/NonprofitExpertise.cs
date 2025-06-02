using System.ComponentModel.DataAnnotations;

namespace GainIt.API.Models.Users.Expertise
{
    public class NonprofitExpertise : UserExpertise
    {
        [Required(ErrorMessage = "Field of work is required")]
        [StringLength(200, ErrorMessage = "Field of work cannot exceed 200 characters")]
        public required string FieldOfWork { get; set; }

        [Required(ErrorMessage = "Organization Mission Statement is required")]
        [StringLength(1000, ErrorMessage = "Mission statement cannot exceed 1000 characters")]
        public required string MissionStatement { get; set; }
    }
}
 