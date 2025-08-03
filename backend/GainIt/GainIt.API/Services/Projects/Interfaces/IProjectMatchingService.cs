using GainIt.API.DTOs.Search;
using GainIt.API.Models.Projects;
using GainIt.API.Models.Users;

namespace GainIt.API.Services.Projects.Interfaces
{
    public interface IProjectMatchingService
    {
        Task<ProjectMatchResultDto> MatchProjectsByTextAsync(string i_InputText, int i_ResultCount = 3);
        Task<IEnumerable<TemplateProject>> MatchProjectsByProfileAsync(Guid i_UserId, int i_ResultCount = 3);
    }
}
