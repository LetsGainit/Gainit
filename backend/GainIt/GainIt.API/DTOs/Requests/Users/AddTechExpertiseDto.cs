using System.ComponentModel.DataAnnotations;

namespace GainIt.API.DTOs.Requests.Users
{
    /// <summary>
    /// DTO for adding technical expertise to a user
    /// </summary>
    public class AddTechExpertiseDto
    {
        /// <summary>
        /// List of programming languages the user knows
        /// </summary>
        [Required(ErrorMessage = "At least one programming language is required")]
        public List<string> ProgrammingLanguages { get; set; } = new();

        /// <summary>
        /// List of technologies/frameworks the user is familiar with
        /// </summary>
        public List<string> Technologies { get; set; } = new();

        /// <summary>
        /// List of tools the user can use
        /// </summary>
        public List<string> Tools { get; set; } = new();
    }
} 