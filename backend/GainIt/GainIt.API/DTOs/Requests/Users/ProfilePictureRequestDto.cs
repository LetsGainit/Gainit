using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace GainIt.API.DTOs.Requests.Users
{
    public class ProfilePictureRequestDto
    {
        [Required]
        public IFormFile ProfilePicture { get; set; } = default!;
        
        [StringLength(500)]
        public string? Description { get; set; }
    }
}
