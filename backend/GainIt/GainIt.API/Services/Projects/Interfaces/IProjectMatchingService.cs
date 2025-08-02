using GainIt.API.Models.Projects;
using GainIt.API.Models.Users;

namespace GainIt.API.Services.Projects.Interfaces
{
    public interface IProjectMatchingService
    {
        Task<IEnumerable<TemplateProject>> MatchProjectsByTextAsync(string i_InputText, int i_ResultCount = 3);
        Task<IEnumerable<TemplateProject>> MatchProjectsByProfileAsync(User i_UserProfile, int i_ResultCount = 3);
    }
}
