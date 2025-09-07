using GainIt.API.Models.Enums.Projects;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace GainIt.API.DTOs.Requests.Projects
{
    public class ProjectUpdateDto
    {
        [StringLength(200, ErrorMessage = "Project Name cannot exceed 200 characters")]
        public string? ProjectName { get; set; }

        [StringLength(1000, ErrorMessage = "Project Description cannot exceed 1000 characters")]
        public string? ProjectDescription { get; set; }

        public eDifficultyLevel? DifficultyLevel { get; set; }

        [Url(ErrorMessage = "Invalid Project Picture URL")]
        [StringLength(500, ErrorMessage = "Project Picture URL cannot exceed 500 characters")]
        public string? ProjectPictureUrl { get; set; }

        /// <summary>
        /// Project picture file upload. If provided, will override ProjectPictureUrl.
        /// </summary>
        public IFormFile? ProjectPicture { get; set; }

        [Url(ErrorMessage = "Invalid Repository URL")]
        public string? RepositoryLink { get; set; }

        [StringLength(2000, ErrorMessage = "Project Goals cannot exceed 2000 characters")]
        public List<string>? Goals { get; set; }

        public List<string>? Technologies { get; set; }

        public List<string>? RequiredRoles { get; set; }

        public List<string>? ProgrammingLanguages { get; set; }

        public eProjectStatus? ProjectStatus { get; set; }
    }
}
