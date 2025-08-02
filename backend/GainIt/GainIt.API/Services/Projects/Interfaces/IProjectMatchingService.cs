using GainIt.API.Models.Projects;
using GainIt.API.Models.Users;

namespace GainIt.API.Services.Projects.Interfaces
{
    public interface IProjectMatchingService
    {
        Task<IEnumerable<TemplateProject>> MatchProjectsByFreeTextAsync(string i_InputText, int i_ResultCount = 3);
        Task<string> MatchWithProfileAndExplainAsync(User i_UserProfile, int i_ResultCount = 3);
    }
}
