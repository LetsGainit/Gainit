using System.ComponentModel.DataAnnotations;

namespace GainIt.API.DTOs.Requests.Users
{
    /// <summary>
    /// DTO for adding nonprofit expertise to a nonprofit organization
    /// </summary>
    public class AddNonprofitExpertiseDto
    {
        /// <summary>
        /// The field of work or industry the nonprofit operates in
        /// </summary>
        [Required(ErrorMessage = "Field of work is required")]
        [StringLength(200, ErrorMessage = "Field of work cannot exceed 200 characters")]
        public string FieldOfWork { get; set; } = string.Empty;

        /// <summary>
        /// The mission statement of the nonprofit organization
        /// </summary>
        [Required(ErrorMessage = "Mission statement is required")]
        [StringLength(1000, ErrorMessage = "Mission statement cannot exceed 1000 characters")]
        public string MissionStatement { get; set; } = string.Empty;
    }
} 