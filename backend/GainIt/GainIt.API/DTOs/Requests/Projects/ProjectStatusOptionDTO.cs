using GainIt.API.Models.Enums.Projects;

namespace GainIt.API.DTOs.Requests.Projects
{
    public class ProjectStatusOptionDTO
    {
        public eProjectStatus ProjectStatus { get; set; }
        public bool IsValid()
        {
            return Enum.IsDefined(typeof(eProjectStatus), ProjectStatus);
        }
    }
}
