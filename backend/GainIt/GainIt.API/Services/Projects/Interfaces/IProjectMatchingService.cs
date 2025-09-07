using GainIt.API.DTOs.Search;
using GainIt.API.DTOs.ViewModels.Projects;
using GainIt.API.Models.Users;

namespace GainIt.API.Services.Projects.Interfaces
{
    public enum GitHubInsightsMode
    {
        QA,
        UserSummary,
        ProjectSummary
    }

    public interface IProjectMatchingService
    {
        Task<ProjectMatchResultDto> MatchProjectsByTextAsync(string i_InputText, int i_ResultCount = 3);
        Task<IEnumerable<AzureVectorSearchProjectViewModel>> MatchProjectsByProfileAsync(Guid i_UserId, int i_ResultCount = 3);
        
        /// <summary>
        /// Generates AI-powered insights for GitHub analytics using the existing GPT configuration
        /// </summary>
        /// <param name="analyticsSummary">Raw GitHub analytics summary</param>
        /// <param name="userQuery">Optional user query for context</param>
        /// <param name="mode">Controls the output style and focus</param>
        /// <param name="daysPeriod">Number of days that the analytics summary covers.</param>
        /// <returns>Enhanced analytics explanation with AI insights</returns>
        Task<string> GetGitHubAnalyticsExplanationAsync(string analyticsSummary, string? userQuery = null, GitHubInsightsMode mode = GitHubInsightsMode.QA, int daysPeriod = 30);
    }
}
