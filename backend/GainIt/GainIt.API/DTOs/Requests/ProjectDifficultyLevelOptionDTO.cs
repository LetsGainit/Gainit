using GainIt.API.Models.Enums.Projects;

namespace GainIt.API.DTOs.Requests
{
    public class ProjectDifficultyLevelOptionDTO
    {
        public eDifficultyLevel DifficultyLevel { get; set; }

        public bool IsValid()
        {
            return Enum.IsDefined(typeof(eDifficultyLevel), DifficultyLevel);
        }
    }
}
