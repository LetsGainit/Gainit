using GainIt.API.Models.Enums.Projects;

namespace GainIt.API.DTOs.Requests.Projects
{
    public class ProjectSourceDTO
    {
        public eProjectSource ProjectSource { get; set; }

        public bool IsValid()
        {
            return Enum.IsDefined(typeof(eProjectSource), ProjectSource);
        }
    }
}
